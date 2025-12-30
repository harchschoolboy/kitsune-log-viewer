using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KitsuneViewer.Services;
using Microsoft.Win32;

namespace KitsuneViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LogPanelViewModel> _logPanels = new();
    
    [ObservableProperty]
    private LogPanelViewModel? _activePanel;
    
    [ObservableProperty]
    private bool _isTimeSyncEnabled;
    
    [ObservableProperty]
    private string _currentThemeName = "Dark (VS Code)";
    
    private readonly TimeSyncService _timeSyncService;
    private readonly ThemeService _themeService;
    private readonly SessionService _sessionService;
    
    public ThemeDefinition[] AvailableThemes => ThemeService.AvailableThemes;
    
    // Events for layout save/restore
    public event Func<string>? GetLayoutRequested;
    public event Action<string>? RestoreLayoutRequested;
    
    public MainViewModel()
    {
        _timeSyncService = TimeSyncService.Instance;
        _themeService = ThemeService.Instance;
        _sessionService = SessionService.Instance;
        
        IsTimeSyncEnabled = _timeSyncService.IsSyncEnabled;
        CurrentThemeName = _themeService.CurrentTheme.Name;
        
        // Apply default theme on startup
        _themeService.ApplyTheme(0);
        
        // Note: RestoreLastSession is called from MainWindow after layout is ready
    }
    
    public void RestoreLastSession()
    {
        var autoSession = _sessionService.GetAutoSession();
        if (autoSession != null && autoSession.FilePaths.Count > 0)
        {
            var existingFiles = autoSession.FilePaths.Where(System.IO.File.Exists).ToList();
            if (existingFiles.Count > 0)
            {
                Logger.Info($"Restoring session with {existingFiles.Count} files");
                foreach (var file in existingFiles)
                {
                    OpenFile(file);
                }
                
                // Restore layout after files are loaded
                if (!string.IsNullOrEmpty(autoSession.LayoutXml))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        RestoreLayoutRequested?.Invoke(autoSession.LayoutXml);
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }
    }
    
    public void SaveCurrentSession()
    {
        var filePaths = LogPanels.Select(p => p.FilePath).ToList();
        var layoutXml = GetLayoutRequested?.Invoke();
        _sessionService.SaveAutoSession(filePaths, layoutXml);
    }
    
    partial void OnIsTimeSyncEnabledChanged(bool value)
    {
        _timeSyncService.IsSyncEnabled = value;
    }
    
    [RelayCommand]
    private void OpenFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Log Files",
            Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
            Multiselect = true
        };
        
        if (dialog.ShowDialog() == true)
        {
            foreach (var filePath in dialog.FileNames)
            {
                OpenFile(filePath);
            }
        }
    }
    
    public async void OpenFile(string filePath)
    {
        // Check if file is already open
        if (LogPanels.Any(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"File is already open: {filePath}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var panel = new LogPanelViewModel();
        
        // Subscribe to ApplyFilterToAll event
        panel.ApplyFilterToAllRequested += OnApplyFilterToAll;
        
        LogPanels.Add(panel);
        ActivePanel = panel;
        
        await panel.LoadFileAsync(filePath);
        
        // Save session after opening file
        SaveCurrentSession();
    }
    
    private void OnApplyFilterToAll(object? sender, string filter)
    {
        foreach (var panel in LogPanels)
        {
            panel.SetFilter(filter);
        }
    }
    
    [RelayCommand]
    private void ClosePanel(LogPanelViewModel? panel)
    {
        if (panel == null) return;
        
        panel.ApplyFilterToAllRequested -= OnApplyFilterToAll;
        panel.Dispose();
        LogPanels.Remove(panel);
        
        if (ActivePanel == panel)
        {
            ActivePanel = LogPanels.FirstOrDefault();
        }
        
        // Save session after closing
        SaveCurrentSession();
    }
    
    [RelayCommand]
    private void CloseAllPanels()
    {
        foreach (var panel in LogPanels.ToList())
        {
            panel.ApplyFilterToAllRequested -= OnApplyFilterToAll;
            panel.Dispose();
        }
        LogPanels.Clear();
        ActivePanel = null;
        SaveCurrentSession();
    }
    
    [RelayCommand]
    private void ToggleTimeSync()
    {
        IsTimeSyncEnabled = !IsTimeSyncEnabled;
    }
    
    [RelayCommand]
    private void SaveSession()
    {
        if (LogPanels.Count == 0)
        {
            MessageBox.Show("No files are open to save as session.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Simple input dialog using WPF
        var defaultName = $"Session_{DateTime.Now:yyyyMMdd_HHmm}";
        var name = ShowInputDialog("Save Session", "Enter session name:", defaultName);
        
        if (!string.IsNullOrWhiteSpace(name))
        {
            var filePaths = LogPanels.Select(p => p.FilePath).ToList();
            var layoutXml = GetLayoutRequested?.Invoke();
            _sessionService.SaveCurrentSession(name, filePaths, layoutXml);
            MessageBox.Show($"Session '{name}' saved with {filePaths.Count} files.", "Session Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private string? ShowInputDialog(string title, string prompt, string defaultValue)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 350,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
            ResizeMode = ResizeMode.NoResize,
            Background = Application.Current.Resources["BackgroundBrush"] as System.Windows.Media.Brush
        };
        
        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Margin = new Thickness(15);
        
        var label = new System.Windows.Controls.TextBlock 
        { 
            Text = prompt, 
            Margin = new Thickness(0, 0, 0, 10),
            Foreground = Application.Current.Resources["ForegroundBrush"] as System.Windows.Media.Brush
        };
        System.Windows.Controls.Grid.SetRow(label, 0);
        
        var textBox = new System.Windows.Controls.TextBox 
        { 
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 15)
        };
        System.Windows.Controls.Grid.SetRow(textBox, 1);
        
        var buttonsPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new System.Windows.Controls.Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
        var cancelButton = new System.Windows.Controls.Button { Content = "Cancel", Width = 75, IsCancel = true };
        buttonsPanel.Children.Add(okButton);
        buttonsPanel.Children.Add(cancelButton);
        System.Windows.Controls.Grid.SetRow(buttonsPanel, 2);
        
        string? result = null;
        okButton.Click += (s, e) => { result = textBox.Text; dialog.DialogResult = true; };
        cancelButton.Click += (s, e) => { dialog.DialogResult = false; };
        
        grid.Children.Add(label);
        grid.Children.Add(textBox);
        grid.Children.Add(buttonsPanel);
        dialog.Content = grid;
        
        textBox.SelectAll();
        textBox.Focus();
        
        return dialog.ShowDialog() == true ? result : null;
    }
    
    public void LoadSession(string name)
    {
        var session = _sessionService.Sessions.FirstOrDefault(s => s.Name == name);
        if (session == null) return;
        
        // Close current files
        CloseAllPanels();
        
        // Open session files
        foreach (var file in session.FilePaths.Where(System.IO.File.Exists))
        {
            OpenFile(file);
        }
        
        // Restore layout after files are loaded
        if (!string.IsNullOrEmpty(session.LayoutXml))
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                RestoreLayoutRequested?.Invoke(session.LayoutXml);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
    
    public IEnumerable<Session> GetUserSessions() => _sessionService.GetUserSessions();
    
    [RelayCommand]
    private void SetTheme(string indexStr)
    {
        if (int.TryParse(indexStr, out int index))
        {
            _themeService.ApplyTheme(index);
            CurrentThemeName = _themeService.CurrentTheme.Name;
        }
    }
    
    [RelayCommand]
    private void NextTheme()
    {
        _themeService.NextTheme();
        CurrentThemeName = _themeService.CurrentTheme.Name;
    }
    
    [RelayCommand]
    private void ShowAbout()
    {
        MessageBox.Show(
            "Kitsune Log Viewer v1.0\n\n" +
            "A multi-file real-time log viewer with:\n" +
            "• File tailing (like tail -f)\n" +
            "• Dockable log panels\n" +
            "• Timestamp-based synchronization\n" +
            "• Log level highlighting\n" +
            "• Multiple themes\n\n" +
            "© 2024",
            "About Kitsune Viewer",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    [RelayCommand]
    private void Exit()
    {
        CloseAllPanels();
        Application.Current.Shutdown();
    }
}
