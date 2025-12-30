using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KitsuneViewer.Services;

public class Session
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastOpenedAt { get; set; }
    public List<string> FilePaths { get; set; } = new();
    public string? LayoutXml { get; set; }
}

public class SessionData
{
    public string? LastSessionName { get; set; }
    public List<Session> Sessions { get; set; } = new();
}

public class SessionService
{
    private static readonly Lazy<SessionService> _instance = new(() => new SessionService());
    public static SessionService Instance => _instance.Value;
    
    private readonly string _sessionsFilePath;
    private SessionData _data = new();
    
    public IReadOnlyList<Session> Sessions => _data.Sessions.AsReadOnly();
    public Session? LastSession => _data.Sessions.FirstOrDefault(s => s.Name == _data.LastSessionName);
    
    private SessionService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KitsuneViewer");
        
        Directory.CreateDirectory(appDataPath);
        _sessionsFilePath = Path.Combine(appDataPath, "sessions.json");
        
        LoadSessions();
    }
    
    private void LoadSessions()
    {
        try
        {
            if (File.Exists(_sessionsFilePath))
            {
                var json = File.ReadAllText(_sessionsFilePath);
                _data = JsonSerializer.Deserialize<SessionData>(json) ?? new SessionData();
                Logger.Info($"Loaded {_data.Sessions.Count} sessions");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load sessions", ex);
            _data = new SessionData();
        }
    }
    
    private void SaveSessions()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_sessionsFilePath, json);
            Logger.Debug("Sessions saved");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to save sessions", ex);
        }
    }
    
    public void SaveCurrentSession(string name, IEnumerable<string> filePaths, string? layoutXml = null)
    {
        var paths = filePaths.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
        if (paths.Count == 0) return;
        
        var existingSession = _data.Sessions.FirstOrDefault(s => s.Name == name);
        
        if (existingSession != null)
        {
            existingSession.FilePaths = paths;
            existingSession.LastOpenedAt = DateTime.Now;
            existingSession.LayoutXml = layoutXml;
        }
        else
        {
            _data.Sessions.Add(new Session
            {
                Name = name,
                CreatedAt = DateTime.Now,
                LastOpenedAt = DateTime.Now,
                FilePaths = paths,
                LayoutXml = layoutXml
            });
        }
        
        _data.LastSessionName = name;
        SaveSessions();
        
        Logger.Info($"Saved session '{name}' with {paths.Count} files");
    }
    
    public void SaveAutoSession(IEnumerable<string> filePaths, string? layoutXml = null)
    {
        SaveCurrentSession("__auto__", filePaths, layoutXml);
    }
    
    public Session? GetAutoSession()
    {
        return _data.Sessions.FirstOrDefault(s => s.Name == "__auto__");
    }
    
    public List<string>? GetLastSessionFiles()
    {
        var autoSession = GetAutoSession();
        if (autoSession != null && autoSession.FilePaths.Count > 0)
        {
            // Filter to only existing files
            return autoSession.FilePaths.Where(File.Exists).ToList();
        }
        return null;
    }
    
    public void DeleteSession(string name)
    {
        var session = _data.Sessions.FirstOrDefault(s => s.Name == name);
        if (session != null)
        {
            _data.Sessions.Remove(session);
            if (_data.LastSessionName == name)
            {
                _data.LastSessionName = null;
            }
            SaveSessions();
            Logger.Info($"Deleted session '{name}'");
        }
    }
    
    public IEnumerable<Session> GetUserSessions()
    {
        return _data.Sessions
            .Where(s => s.Name != "__auto__")
            .OrderByDescending(s => s.LastOpenedAt);
    }
}
