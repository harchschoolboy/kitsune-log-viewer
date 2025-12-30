using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KitsuneViewer.Services;

/// <summary>
/// Service for monitoring file changes (similar to 'tail -f' in Unix)
/// </summary>
public class FileTailService : ObservableObject, IDisposable
{
    private FileSystemWatcher? _watcher;
    private string _filePath = string.Empty;
    private long _lastPosition;
    private readonly SynchronizationContext? _syncContext;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;
    private readonly object _readLock = new();
    
    public event EventHandler<string>? NewLinesReceived;
    public event EventHandler<Exception>? ErrorOccurred;
    
    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set => SetProperty(ref _isMonitoring, value);
    }
    
    private string _fileName = string.Empty;
    public string FileName
    {
        get => _fileName;
        private set => SetProperty(ref _fileName, value);
    }
    
    public FileTailService()
    {
        _syncContext = SynchronizationContext.Current;
    }
    
    public async Task<string> StartMonitoringAsync(string filePath, bool readFromStart = false)
    {
        Logger.Info($"StartMonitoringAsync called for: {filePath}");
        
        if (_isDisposed)
        {
            Logger.Error("FileTailService is already disposed!");
            throw new ObjectDisposedException(nameof(FileTailService));
        }
        
        StopMonitoring();
        
        _filePath = filePath;
        FileName = Path.GetFileName(filePath);
        _cts = new CancellationTokenSource();
        
        var initialContent = new StringBuilder();
        
        try
        {
            // Read initial content
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                if (readFromStart)
                {
                    initialContent.Append(await reader.ReadToEndAsync());
                }
                else
                {
                    // Read last N lines for initial display
                    const int initialLines = 100;
                    var lines = await ReadLastLinesAsync(fs, reader, initialLines);
                    initialContent.Append(lines);
                }
                
                _lastPosition = fs.Position;
            }
            
            // Setup file watcher
            var directory = Path.GetDirectoryName(filePath) ?? ".";
            var fileName = Path.GetFileName(filePath);
            
            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            
            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnWatcherError;
            
            IsMonitoring = true;
            Logger.Info($"Now monitoring: {filePath}");
            
            // Start periodic check (as backup for watcher)
            _ = PeriodicCheckAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start monitoring: {filePath}", ex);
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
        
        return initialContent.ToString();
    }
    
    private async Task<string> ReadLastLinesAsync(FileStream fs, StreamReader reader, int lineCount)
    {
        if (fs.Length == 0) return string.Empty;
        
        const int bufferSize = 4096;
        var lines = new System.Collections.Generic.List<string>();
        var buffer = new byte[bufferSize];
        var leftover = string.Empty;
        var position = fs.Length;
        
        while (position > 0 && lines.Count < lineCount)
        {
            var bytesToRead = (int)Math.Min(bufferSize, position);
            position -= bytesToRead;
            fs.Seek(position, SeekOrigin.Begin);
            
            await fs.ReadAsync(buffer.AsMemory(0, bytesToRead));
            
            var chunk = Encoding.UTF8.GetString(buffer, 0, bytesToRead) + leftover;
            var chunkLines = chunk.Split('\n');
            
            leftover = chunkLines[0];
            
            for (int i = chunkLines.Length - 1; i > 0; i--)
            {
                var line = chunkLines[i].TrimEnd('\r');
                if (!string.IsNullOrEmpty(line) || lines.Count > 0)
                {
                    lines.Insert(0, line);
                    if (lines.Count >= lineCount) break;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(leftover) && lines.Count < lineCount)
        {
            lines.Insert(0, leftover.TrimEnd('\r'));
        }
        
        // Reset position to end
        fs.Seek(0, SeekOrigin.End);
        
        return string.Join(Environment.NewLine, lines);
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_cts?.IsCancellationRequested == true) return;
        
        Task.Run(async () =>
        {
            // Small delay to ensure file is written
            await Task.Delay(50);
            ReadNewContent();
        });
    }
    
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _syncContext?.Post(_ => ErrorOccurred?.Invoke(this, ex), null);
    }
    
    private async Task PeriodicCheckAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, ct);
                ReadNewContent();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Ignore periodic check errors
            }
        }
    }
    
    private void ReadNewContent()
    {
        if (_isDisposed || string.IsNullOrEmpty(_filePath)) return;
        
        lock (_readLock)
        {
            try
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                
                // Handle file truncation (e.g., log rotation)
                if (fs.Length < _lastPosition)
                {
                    _lastPosition = 0;
                }
                
                if (fs.Length > _lastPosition)
                {
                    fs.Seek(_lastPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    var newContent = reader.ReadToEnd();
                    _lastPosition = fs.Position;
                    
                    if (!string.IsNullOrEmpty(newContent))
                    {
                        _syncContext?.Post(_ => NewLinesReceived?.Invoke(this, newContent), null);
                    }
                }
            }
            catch (Exception ex)
            {
                _syncContext?.Post(_ => ErrorOccurred?.Invoke(this, ex), null);
            }
        }
    }
    
    public void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;
        }
        
        IsMonitoring = false;
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        StopMonitoring();
    }
}
