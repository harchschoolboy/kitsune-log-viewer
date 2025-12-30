using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        
        // Update ViewModel with selected items for copy command
        ViewModel.SelectedEntries = LogListBox.SelectedItems.Cast<LogEntry>().ToList();
        
        if (LogListBox.SelectedItem is LogEntry entry && entry.Timestamp.HasValue)
        {
            Logger.Debug($"User selected line {entry.LineNumber}, syncing timestamp {entry.Timestamp.Value:HH:mm:ss.fff}");
            ViewModel.NotifyTimeSync(entry.Timestamp.Value);
        }
    }
    
    private void LogListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ViewModel?.CopySelectedCommand?.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            LogListBox.SelectAll();
            e.Handled = true;
        }
    }
    
    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        LogListBox.SelectAll();
    }
}
