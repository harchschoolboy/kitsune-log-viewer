using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace KitsuneViewer.Services;

/// <summary>
/// Service for synchronizing log views by timestamp
/// </summary>
public class TimeSyncService : ObservableObject
{
    private static readonly Lazy<TimeSyncService> _instance = new(() => new TimeSyncService());
    public static TimeSyncService Instance => _instance.Value;
    
    private bool _isSyncEnabled;
    public bool IsSyncEnabled
    {
        get => _isSyncEnabled;
        set => SetProperty(ref _isSyncEnabled, value);
    }
    
    private DateTime? _currentSyncTime;
    public DateTime? CurrentSyncTime
    {
        get => _currentSyncTime;
        private set => SetProperty(ref _currentSyncTime, value);
    }
    
    private object? _currentSender;
    
    public event EventHandler<TimeSyncEventArgs>? SyncTimeChanged;
    
    private TimeSyncService() 
    {
        Logger.Debug("TimeSyncService created");
    }
    
    public void BroadcastTime(DateTime timestamp, object sender)
    {
        if (!IsSyncEnabled)
        {
            Logger.Debug("BroadcastTime: sync disabled, ignoring");
            return;
        }
        
        Logger.Debug($"BroadcastTime: {timestamp:HH:mm:ss.fff} from {sender.GetType().Name}");
        
        _currentSender = sender;
        CurrentSyncTime = timestamp;
        
        SyncTimeChanged?.Invoke(this, new TimeSyncEventArgs(timestamp, sender));
    }
    
    public bool IsSenderCurrent(object sender) => ReferenceEquals(_currentSender, sender);
    
    public void Reset()
    {
        CurrentSyncTime = null;
        _currentSender = null;
    }
}

public class TimeSyncEventArgs : EventArgs
{
    public DateTime Timestamp { get; }
    public object Sender { get; }
    
    public TimeSyncEventArgs(DateTime timestamp, object sender)
    {
        Timestamp = timestamp;
        Sender = sender;
    }
}
