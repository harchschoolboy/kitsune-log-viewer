using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace KitsuneViewer.Services;

/// <summary>
/// Simple file logger for debugging
/// </summary>
public static class Logger
{
    private static readonly string LogFilePath;
    private static readonly object Lock = new();
    
    static Logger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KitsuneViewer", "Logs");
        
        Directory.CreateDirectory(logDir);
        
        LogFilePath = Path.Combine(logDir, $"kitsune_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        
        Info($"=== Kitsune Log Viewer Started ===");
        Info($"Log file: {LogFilePath}");
        Info($"OS: {Environment.OSVersion}");
        Info($".NET: {Environment.Version}");
    }
    
    public static string GetLogFilePath() => LogFilePath;
    
    public static void Info(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log("INFO", message, memberName, filePath, lineNumber);
    }
    
    public static void Debug(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log("DEBUG", message, memberName, filePath, lineNumber);
    }
    
    public static void Warning(string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Log("WARN", message, memberName, filePath, lineNumber);
    }
    
    public static void Error(string message, Exception? ex = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fullMessage = ex != null ? $"{message}\n{ex}" : message;
        Log("ERROR", fullMessage, memberName, filePath, lineNumber);
    }
    
    private static void Log(string level, string message, string memberName, string filePath, int lineNumber)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level,-5}] [{fileName}:{lineNumber} {memberName}] {message}";
            
            lock (Lock)
            {
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
            
            // Also write to debug output
            System.Diagnostics.Debug.WriteLine(logLine);
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
