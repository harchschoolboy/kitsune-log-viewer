using System.Windows;
using System.Windows.Controls;
using KitsuneViewer.Models;
using KitsuneViewer.Services;
using KitsuneViewer.ViewModels;

namespace KitsuneViewer.Views;

public partial class LogPanelView : UserControl
{
    private LogPanelViewModel? ViewModel => DataContext as LogPanelViewModel;
    private bool _isScrollingToEntry; // Prevent sync loop
    
    public LogPanelView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LogPanelViewModel oldVm)
        {
            oldVm.ScrollToEndRequested -= OnScrollToEndRequested;
            oldVm.ScrollToEntryRequested -= OnScrollToEntryRequested;
        }
        
        if (e.NewValue is LogPanelViewModel newVm)
        {
            newVm.ScrollToEndRequested += OnScrollToEndRequested;
            newVm.ScrollToEntryRequested += OnScrollToEntryRequested;
        }
    }
    
    private void OnScrollToEndRequested(object? sender, System.EventArgs e)
    {
        if (LogListBox.Items.Count > 0)
        {
            LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
        }
    }
    
    private void OnScrollToEntryRequested(object? sender, LogEntry entry)
    {
        try
        {
            _isScrollingToEntry = true;
            LogListBox.ScrollIntoView(entry);
            LogListBox.SelectedItem = entry;
        }
        finally
        {
            _isScrollingToEntry = false;
        }
    }
    
    private void LogListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Don't trigger sync if we're programmatically scrolling
        if (_isScrollingToEntry) return;
        if (ViewModel == null) return;
        
        if (LogListBox.SelectedItem is LogEntry entry && entry.Timestamp.HasValue)
        {
            Logger.Debug($"User selected line {entry.LineNumber}, syncing timestamp {entry.Timestamp.Value:HH:mm:ss.fff}");
            ViewModel.NotifyTimeSync(entry.Timestamp.Value);
        }
    }
}
