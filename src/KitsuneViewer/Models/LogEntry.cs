using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KitsuneViewer.Models;

/// <summary>
/// Represents a single log entry with parsed timestamp and content
/// </summary>
public partial class LogEntry : ObservableObject
{
    public DateTime? Timestamp { get; set; }
    public string RawLine { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public int LineNumber { get; set; }
    
    public string DisplayText => $"{LineNumber,5} │ {(Timestamp.HasValue ? $"[{Timestamp.Value:HH:mm:ss.fff}] " : "")}{Content}";
    
    public SolidColorBrush LevelBrush
    {
        get
        {
            var key = Level switch
            {
                LogLevel.Error => "ErrorBrush",
                LogLevel.Warning => "WarningBrush",
                LogLevel.Debug => "DebugBrush",
                LogLevel.Trace => "TraceBrush",
                _ => "ForegroundBrush"
            };
            
            return Application.Current.Resources[key] as SolidColorBrush 
                ?? new SolidColorBrush(Colors.White);
        }
    }
    
    // Common timestamp patterns
    private static readonly List<(Regex Regex, string Format)> TimestampPatterns = new()
    {
        // ISO 8601: 2024-01-15T14:30:45.123Z or 2024-01-15T14:30:45
        (new Regex(@"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})?)", RegexOptions.Compiled), ""),
        
        // Standard: 2024-01-15 14:30:45.123 or 2024-01-15 14:30:45
        (new Regex(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[.,]\d{1,7})?)", RegexOptions.Compiled), ""),
        
        // With brackets: [2024-01-15 14:30:45]
        (new Regex(@"^\[(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:[.,]\d{1,7})?)\]", RegexOptions.Compiled), ""),
        
        // Log4j style: 15 Jan 2024 14:30:45,123
        (new Regex(@"^(\d{2}\s+\w{3}\s+\d{4}\s+\d{2}:\d{2}:\d{2}(?:[.,]\d{1,3})?)", RegexOptions.Compiled), ""),
        
        // Unix timestamp (seconds)
        (new Regex(@"^(\d{10})(?:\.\d+)?", RegexOptions.Compiled), "unix"),
        
        // Unix timestamp (milliseconds)
        (new Regex(@"^(\d{13})", RegexOptions.Compiled), "unix_ms"),
    };
    
    // Log level patterns
    private static readonly Regex LogLevelRegex = new(
        @"\b(TRACE|DEBUG|INFO|WARN(?:ING)?|ERROR|FATAL|CRITICAL|ERR|WRN|INF|DBG|TRC)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static LogEntry Parse(string line, int lineNumber)
    {
        var entry = new LogEntry
        {
            RawLine = line,
            Content = line,
            LineNumber = lineNumber
        };
        
        // Try to parse timestamp
        foreach (var (regex, format) in TimestampPatterns)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var timestampStr = match.Groups[1].Value;
                
                if (format == "unix" && long.TryParse(timestampStr, out var unixSeconds))
                {
                    entry.Timestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).LocalDateTime;
                }
                else if (format == "unix_ms" && long.TryParse(timestampStr, out var unixMs))
                {
                    entry.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
                }
                else if (DateTime.TryParse(timestampStr.Replace(',', '.'), out var dt))
                {
                    entry.Timestamp = dt;
                }
                
                if (entry.Timestamp.HasValue)
                {
                    entry.Content = line.Substring(match.Length).TrimStart(' ', ':', '-', '|');
                    break;
                }
            }
        }
        
        // Try to parse log level
        var levelMatch = LogLevelRegex.Match(line);
        if (levelMatch.Success)
        {
            entry.Level = ParseLogLevel(levelMatch.Groups[1].Value);
        }
        
        return entry;
    }
    
    private static LogLevel ParseLogLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "TRACE" or "TRC" => LogLevel.Trace,
            "DEBUG" or "DBG" => LogLevel.Debug,
            "INFO" or "INF" or "INFORMATION" => LogLevel.Info,
            "WARN" or "WARNING" or "WRN" => LogLevel.Warning,
            "ERROR" or "ERR" or "FATAL" or "CRITICAL" => LogLevel.Error,
            _ => LogLevel.Info
        };
    }
}

public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error
}
