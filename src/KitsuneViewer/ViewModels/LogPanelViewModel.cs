using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KitsuneViewer.Models;
using KitsuneViewer.Services;

namespace KitsuneViewer.ViewModels;

public partial class LogPanelViewModel : ObservableObject, IDisposable
{
    private readonly FileTailService _tailService;
    private readonly TimeSyncService _timeSyncService;
    private bool _isDisposed;
    private int _lineCounter;
    private readonly object _entriesLock = new();
    
    [ObservableProperty]
    private string _title = "New Log";
    
    [ObservableProperty]
    private string _filePath = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<LogEntry> _entries = new();
    
    [ObservableProperty]
    private ICollectionView? _filteredEntries;
    
    [ObservableProperty]
    private bool _isFollowing = true;
    
    [ObservableProperty]
    private bool _isPaused;
    
    [ObservableProperty]
    private string _filterText = string.Empty;
    
    [ObservableProperty]
    private bool _isWordWrap;
    
    [ObservableProperty]
    private bool _showTimestamps = true;
    
    [ObservableProperty]
    private bool _isTimeSync = true;
    
    [ObservableProperty]
    private string _statusText = "Ready";
    
    [ObservableProperty]
    private int _totalLines;
    
    public event EventHandler? ScrollToEndRequested;
    public event EventHandler<LogEntry>? ScrollToEntryRequested;
    public event EventHandler<string>? ApplyFilterToAllRequested;
    
    public LogPanelViewModel()
    {
        Logger.Debug("Creating new LogPanelViewModel");
        
        _tailService = new FileTailService();
        _timeSyncService = TimeSyncService.Instance;
        
        _tailService.NewLinesReceived += OnNewLinesReceived;
        _tailService.ErrorOccurred += OnErrorOccurred;
        _timeSyncService.SyncTimeChanged += OnSyncTimeChanged;
        
        // Setup filtered view
        FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
        FilteredEntries.Filter = FilterEntry;
        
        Logger.Debug("LogPanelViewModel created");
    }
    
    private bool FilterEntry(object obj)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;
        if (obj is LogEntry entry)
        {
            return entry.RawLine.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
    
    partial void OnFilterTextChanged(string value)
    {
        // Auto-apply filter when text changes
        FilteredEntries?.Refresh();
    }
    
    partial void OnIsWordWrapChanged(bool value)
    {
        // Notify UI about word wrap change
        OnPropertyChanged(nameof(IsWordWrap));
    }
    
    public async Task LoadFileAsync(string path)
    {
        Logger.Info($"LoadFileAsync: {path}");
        
        try
        {
            FilePath = path;
            Title = Path.GetFileName(path);
            StatusText = "Loading...";
            
            _lineCounter = 0;
            
            Logger.Debug("Clearing entries...");
            Application.Current.Dispatcher.Invoke(() =>
            {
                Entries.Clear();
            });
            
            Logger.Debug("Starting file monitoring...");
            var initialContent = await _tailService.StartMonitoringAsync(path, readFromStart: false);
            
            if (!string.IsNullOrEmpty(initialContent))
            {
                Logger.Debug($"Got initial content: {initialContent.Length} chars");
                AppendContent(initialContent);
            }
            
            Logger.Info($"Successfully loaded: {path}");
            StatusText = $"Monitoring: {Path.GetFileName(FilePath)}";
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load file: {path}", ex);
            StatusText = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to load file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void OnNewLinesReceived(object? sender, string newContent)
    {
        if (IsPaused) return;
        
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AppendContent(newContent);
            
                if (IsFollowing)
                {
                    ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Error processing new lines", ex);
        }
    }
    
    private void AppendContent(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            
            _lineCounter++;
            var entry = LogEntry.Parse(line, _lineCounter);
            Entries.Add(entry);
            
            // Keep entries manageable (max 50000)
            while (Entries.Count > 50000)
            {
                Entries.RemoveAt(0);
            }
        }
        
        TotalLines = _lineCounter;
    }
    
    private void OnErrorOccurred(object? sender, Exception ex)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = $"Error: {ex.Message}";
        });
    }
    
    private void OnSyncTimeChanged(object? sender, TimeSyncEventArgs e)
    {
        // Don't react to our own sync events
        if (!IsTimeSync || _timeSyncService.IsSenderCurrent(this)) 
        {
            Logger.Debug($"OnSyncTimeChanged: ignoring (IsTimeSync={IsTimeSync}, isSelf={_timeSyncService.IsSenderCurrent(this)})");
            return;
        }
        
        Logger.Debug($"OnSyncTimeChanged: syncing to {e.Timestamp:HH:mm:ss.fff}");
        
        try
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Find closest entry to the timestamp
                var closestEntry = Entries
                    .Where(ent => ent.Timestamp.HasValue)
                    .OrderBy(ent => Math.Abs((ent.Timestamp!.Value - e.Timestamp).TotalMilliseconds))
                    .FirstOrDefault();
                
                if (closestEntry != null)
                {
                    Logger.Debug($"OnSyncTimeChanged: scrolling to line {closestEntry.LineNumber}");
                    ScrollToEntryRequested?.Invoke(this, closestEntry);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.Error("Error in OnSyncTimeChanged", ex);
        }
    }
    
    [RelayCommand]
    private void ToggleFollow()
    {
        IsFollowing = !IsFollowing;
        if (IsFollowing)
        {
            ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    
    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        StatusText = IsPaused ? "Paused" : $"Monitoring: {Path.GetFileName(FilePath)}";
    }
    
    [RelayCommand]
    private void Clear()
    {
        Entries.Clear();
        _lineCounter = 0;
        TotalLines = 0;
    }
    
    [RelayCommand]
    private void CopyToClipboard()
    {
        var text = string.Join(Environment.NewLine, Entries.Select(e => e.RawLine));
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }
    
    [RelayCommand]
    private void ApplyFilterToAll()
    {
        ApplyFilterToAllRequested?.Invoke(this, FilterText);
    }
    
    public void SetFilter(string filter)
    {
        FilterText = filter;
    }
    
    public void NotifyTimeSync(DateTime timestamp)
    {
        if (IsTimeSync)
        {
            _timeSyncService.BroadcastTime(timestamp, this);
        }
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        _tailService.NewLinesReceived -= OnNewLinesReceived;
        _tailService.ErrorOccurred -= OnErrorOccurred;
        _timeSyncService.SyncTimeChanged -= OnSyncTimeChanged;
        
        _tailService.Dispose();
    }
}
