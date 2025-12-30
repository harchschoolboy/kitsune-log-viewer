using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using KitsuneViewer.Services;
using KitsuneViewer.ViewModels;

namespace KitsuneViewer.Views;

public enum DockPosition
{
    Center,
    Left,
    Right,
    Top,
    Bottom
}

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    
    // Dock overlay elements
    private Canvas? _dockOverlay;
    private Border? _previewBorder;
    private DockPosition _currentDockPosition = DockPosition.Center;
    private bool _isDraggingFile = false;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Enable drag and drop
        AllowDrop = true;
        Drop += OnFileDrop;
        DragOver += OnDragOver;
        DragEnter += OnDragEnter;
        DragLeave += OnDragLeave;
        
        Loaded += OnWindowLoaded;
        
        // Setup sessions menu
        SessionsMenu.SubmenuOpened += OnSessionsMenuOpened;
        
        // Setup layout serialization
        ViewModel.GetLayoutRequested += GetLayoutXml;
        ViewModel.RestoreLayoutRequested += RestoreLayoutFromXml;
    }
    
    private string GetLayoutXml()
    {
        try
        {
            var serializer = new XmlLayoutSerializer(DockingManager);
            using var writer = new StringWriter();
            serializer.Serialize(writer);
            return writer.ToString();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to serialize layout", ex);
            return string.Empty;
        }
    }
    
    private void RestoreLayoutFromXml(string layoutXml)
    {
        if (string.IsNullOrEmpty(layoutXml)) return;
        
        try
        {
            var serializer = new XmlLayoutSerializer(DockingManager);
            serializer.LayoutSerializationCallback += (sender, args) =>
            {
                // Find the panel by file path (stored as ContentId)
                var panel = ViewModel.LogPanels.FirstOrDefault(p => p.FilePath == args.Model.ContentId);
                if (panel != null)
                {
                    args.Content = panel;
                }
                else
                {
                    args.Cancel = true;
                }
            };
            
            using var reader = new StringReader(layoutXml);
            serializer.Deserialize(reader);
            
            Logger.Info("Layout restored successfully");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to restore layout", ex);
        }
    }
    
    private void OnSessionsMenuOpened(object sender, RoutedEventArgs e)
    {
        SessionsMenu.Items.Clear();
        
        var sessions = ViewModel.GetUserSessions().ToList();
        
        if (sessions.Count == 0)
        {
            var emptyItem = new MenuItem { Header = "(No saved sessions)", IsEnabled = false };
            SessionsMenu.Items.Add(emptyItem);
        }
        else
        {
            foreach (var session in sessions)
            {
                var filesCount = session.FilePaths.Count;
                var menuItem = new MenuItem
                {
                    Header = $"{session.Name} ({filesCount} files)",
                    ToolTip = $"Last opened: {session.LastOpenedAt:g}\n{string.Join("\n", session.FilePaths.Select(System.IO.Path.GetFileName))}"
                };
                
                var sessionName = session.Name;
                menuItem.Click += (s, args) => ViewModel.LoadSession(sessionName);
                
                SessionsMenu.Items.Add(menuItem);
            }
        }
    }
    
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        CreateDockOverlay();
        
        // Restore last session after window is ready
        ViewModel.RestoreLastSession();
    }
    
    private void CreateDockOverlay()
    {
        // Create overlay canvas for dock indicators
        _dockOverlay = new Canvas
        {
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        
        // Preview border showing where file will be docked
        _previewBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(60, 14, 99, 156)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(14, 99, 156)),
            BorderThickness = new Thickness(2),
            Visibility = Visibility.Collapsed
        };
        
        // Add dock indicator buttons
        CreateDockIndicators();
        
        // Add overlay to the main grid
        var mainGrid = Content as Grid;
        if (mainGrid != null)
        {
            mainGrid.Children.Add(_dockOverlay);
            mainGrid.Children.Add(_previewBorder);
            Grid.SetRowSpan(_dockOverlay, 4);
            Grid.SetRowSpan(_previewBorder, 4);
        }
    }
    
    private void CreateDockIndicators()
    {
        if (_dockOverlay == null) return;
        
        // Center indicator
        var centerIndicator = CreateIndicator("⊞", DockPosition.Center);
        _dockOverlay.Children.Add(centerIndicator);
        
        // Directional indicators
        var leftIndicator = CreateIndicator("◀", DockPosition.Left);
        var rightIndicator = CreateIndicator("▶", DockPosition.Right);
        var topIndicator = CreateIndicator("▲", DockPosition.Top);
        var bottomIndicator = CreateIndicator("▼", DockPosition.Bottom);
        
        _dockOverlay.Children.Add(leftIndicator);
        _dockOverlay.Children.Add(rightIndicator);
        _dockOverlay.Children.Add(topIndicator);
        _dockOverlay.Children.Add(bottomIndicator);
    }
    
    private Border CreateIndicator(string symbol, DockPosition position)
    {
        var indicator = new Border
        {
            Width = 40,
            Height = 40,
            Background = new SolidColorBrush(Color.FromArgb(220, 45, 45, 48)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Tag = position,
            Child = new TextBlock
            {
                Text = symbol,
                FontSize = 20,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        
        return indicator;
    }
    
    private void PositionIndicators()
    {
        if (_dockOverlay == null) return;
        
        // Get DockingManager bounds
        var dockManagerBounds = DockingManager.TransformToAncestor(this)
            .TransformBounds(new Rect(0, 0, DockingManager.ActualWidth, DockingManager.ActualHeight));
        
        var centerX = dockManagerBounds.Left + dockManagerBounds.Width / 2;
        var centerY = dockManagerBounds.Top + dockManagerBounds.Height / 2;
        
        foreach (Border indicator in _dockOverlay.Children.OfType<Border>())
        {
            var position = (DockPosition)indicator.Tag;
            double left = 0, top = 0;
            
            switch (position)
            {
                case DockPosition.Center:
                    left = centerX - 20;
                    top = centerY - 20;
                    break;
                case DockPosition.Left:
                    left = centerX - 80;
                    top = centerY - 20;
                    break;
                case DockPosition.Right:
                    left = centerX + 40;
                    top = centerY - 20;
                    break;
                case DockPosition.Top:
                    left = centerX - 20;
                    top = centerY - 80;
                    break;
                case DockPosition.Bottom:
                    left = centerX - 20;
                    top = centerY + 40;
                    break;
            }
            
            Canvas.SetLeft(indicator, left);
            Canvas.SetTop(indicator, top);
        }
    }
    
    private void UpdatePreviewArea(DockPosition position)
    {
        if (_previewBorder == null) return;
        
        var dockManagerBounds = DockingManager.TransformToAncestor(this)
            .TransformBounds(new Rect(0, 0, DockingManager.ActualWidth, DockingManager.ActualHeight));
        
        double left = dockManagerBounds.Left;
        double top = dockManagerBounds.Top;
        double width = dockManagerBounds.Width;
        double height = dockManagerBounds.Height;
        
        switch (position)
        {
            case DockPosition.Left:
                width = dockManagerBounds.Width / 2;
                break;
            case DockPosition.Right:
                left = dockManagerBounds.Left + dockManagerBounds.Width / 2;
                width = dockManagerBounds.Width / 2;
                break;
            case DockPosition.Top:
                height = dockManagerBounds.Height / 2;
                break;
            case DockPosition.Bottom:
                top = dockManagerBounds.Top + dockManagerBounds.Height / 2;
                height = dockManagerBounds.Height / 2;
                break;
        }
        
        _previewBorder.Margin = new Thickness(left, top, 0, 0);
        _previewBorder.Width = width;
        _previewBorder.Height = height;
        _previewBorder.HorizontalAlignment = HorizontalAlignment.Left;
        _previewBorder.VerticalAlignment = VerticalAlignment.Top;
        _previewBorder.Visibility = Visibility.Visible;
    }
    
    private DockPosition GetDockPositionFromPoint(Point point)
    {
        if (_dockOverlay == null) return DockPosition.Center;
        
        foreach (Border indicator in _dockOverlay.Children.OfType<Border>())
        {
            var indicatorBounds = new Rect(
                Canvas.GetLeft(indicator),
                Canvas.GetTop(indicator),
                indicator.Width,
                indicator.Height);
            
            if (indicatorBounds.Contains(point))
            {
                return (DockPosition)indicator.Tag;
            }
        }
        
        return DockPosition.Center;
    }
    
    private void HighlightIndicator(DockPosition position)
    {
        if (_dockOverlay == null) return;
        
        foreach (Border indicator in _dockOverlay.Children.OfType<Border>())
        {
            var pos = (DockPosition)indicator.Tag;
            if (pos == position)
            {
                indicator.Background = new SolidColorBrush(Color.FromRgb(14, 99, 156));
                indicator.BorderBrush = new SolidColorBrush(Color.FromRgb(17, 119, 187));
            }
            else
            {
                indicator.Background = new SolidColorBrush(Color.FromArgb(220, 45, 45, 48));
                indicator.BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70));
            }
        }
    }
    
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            _isDraggingFile = true;
            
            if (_dockOverlay != null && ViewModel.LogPanels.Count > 0)
            {
                _dockOverlay.Visibility = Visibility.Visible;
                PositionIndicators();
            }
        }
    }
    
    private void OnDragLeave(object sender, DragEventArgs e)
    {
        // Check if actually left the window
        var pos = e.GetPosition(this);
        if (pos.X < 0 || pos.Y < 0 || pos.X > ActualWidth || pos.Y > ActualHeight)
        {
            HideDockOverlay();
        }
    }
    
    private void HideDockOverlay()
    {
        _isDraggingFile = false;
        if (_dockOverlay != null)
        {
            _dockOverlay.Visibility = Visibility.Collapsed;
        }
        if (_previewBorder != null)
        {
            _previewBorder.Visibility = Visibility.Collapsed;
        }
    }
    
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            
            if (_isDraggingFile && _dockOverlay != null && ViewModel.LogPanels.Count > 0)
            {
                var pos = e.GetPosition(this);
                _currentDockPosition = GetDockPositionFromPoint(pos);
                HighlightIndicator(_currentDockPosition);
                UpdatePreviewArea(_currentDockPosition);
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
    
    private void OnFileDrop(object sender, DragEventArgs e)
    {
        var dockPosition = _currentDockPosition;
        HideDockOverlay();
        
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var file in files.Where(f => File.Exists(f)))
                {
                    if (ViewModel.LogPanels.Count == 0)
                    {
                        // First file - just open normally
                        ViewModel.OpenFile(file);
                    }
                    else
                    {
                        // Open with dock position
                        OpenFileWithDocking(file, dockPosition);
                    }
                }
            }
        }
    }
    
    private void OpenFileWithDocking(string filePath, DockPosition position)
    {
        // Check if file is already open
        if (ViewModel.LogPanels.Any(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"File is already open: {filePath}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Create new panel
        var panel = new LogPanelViewModel();
        ViewModel.LogPanels.Add(panel);
        
        // Apply docking position
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            ApplyDockPosition(panel, position);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
        
        // Load file
        _ = panel.LoadFileAsync(filePath);
        ViewModel.ActivePanel = panel;
    }
    
    private void ApplyDockPosition(LogPanelViewModel panel, DockPosition position)
    {
        if (position == DockPosition.Center) return;
        
        // Find the LayoutDocumentPane containing the new document
        var layout = DockingManager.Layout;
        LayoutDocumentPane? sourcePane = null;
        LayoutDocument? newDoc = null;
        
        // Find the document for this panel
        foreach (var doc in layout.Descendents().OfType<LayoutDocument>())
        {
            if (doc.Content == panel)
            {
                newDoc = doc;
                sourcePane = doc.Parent as LayoutDocumentPane;
                break;
            }
        }
        
        if (newDoc == null || sourcePane == null) return;
        
        // Remove from current pane
        sourcePane.Children.Remove(newDoc);
        
        // Create new pane for the document
        var newPane = new LayoutDocumentPane(newDoc);
        
        // Find root panel and add new pane in correct position
        var rootPanel = layout.Descendents().OfType<LayoutPanel>().FirstOrDefault();
        if (rootPanel == null) return;
        
        // Determine orientation and position
        switch (position)
        {
            case DockPosition.Left:
                if (rootPanel.Orientation != Orientation.Horizontal)
                {
                    WrapInHorizontalPanel(rootPanel);
                    rootPanel = layout.Descendents().OfType<LayoutPanel>().First(p => p.Orientation == Orientation.Horizontal);
                }
                rootPanel.Children.Insert(0, newPane);
                break;
                
            case DockPosition.Right:
                if (rootPanel.Orientation != Orientation.Horizontal)
                {
                    WrapInHorizontalPanel(rootPanel);
                    rootPanel = layout.Descendents().OfType<LayoutPanel>().First(p => p.Orientation == Orientation.Horizontal);
                }
                rootPanel.Children.Add(newPane);
                break;
                
            case DockPosition.Top:
                if (rootPanel.Orientation != Orientation.Vertical)
                {
                    WrapInVerticalPanel(rootPanel);
                    rootPanel = layout.Descendents().OfType<LayoutPanel>().First(p => p.Orientation == Orientation.Vertical);
                }
                rootPanel.Children.Insert(0, newPane);
                break;
                
            case DockPosition.Bottom:
                if (rootPanel.Orientation != Orientation.Vertical)
                {
                    WrapInVerticalPanel(rootPanel);
                    rootPanel = layout.Descendents().OfType<LayoutPanel>().First(p => p.Orientation == Orientation.Vertical);
                }
                rootPanel.Children.Add(newPane);
                break;
        }
    }
    
    private void WrapInHorizontalPanel(LayoutPanel panel)
    {
        var children = panel.Children.ToList();
        panel.Children.Clear();
        panel.Orientation = Orientation.Horizontal;
        
        var innerPanel = new LayoutPanel { Orientation = Orientation.Vertical };
        foreach (var child in children)
        {
            innerPanel.Children.Add(child);
        }
        panel.Children.Add(innerPanel);
    }
    
    private void WrapInVerticalPanel(LayoutPanel panel)
    {
        var children = panel.Children.ToList();
        panel.Children.Clear();
        panel.Orientation = Orientation.Vertical;
        
        var innerPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
        foreach (var child in children)
        {
            innerPanel.Children.Add(child);
        }
        panel.Children.Add(innerPanel);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Clean up all panels
        foreach (var panel in ViewModel.LogPanels)
        {
            panel.Dispose();
        }
        
        base.OnClosed(e);
    }
}
