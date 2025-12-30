using System;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KitsuneViewer.Services;

public partial class ThemeService : ObservableObject
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;
    
    public static readonly ThemeDefinition[] AvailableThemes = new[]
    {
        new ThemeDefinition("Dark (VS Code)", ThemeType.Dark,
            background: "#1E1E1E",
            secondaryBackground: "#252526",
            tertiaryBackground: "#2D2D30",
            border: "#3C3C3C",
            foreground: "#D4D4D4",
            foregroundDim: "#808080",
            accent: "#0E639C",
            accentHover: "#1177BB",
            selection: "#264F78",
            lineNumber: "#858585",
            error: "#F14C4C",
            warning: "#CCA700",
            info: "#3794FF",
            debug: "#89D185",
            trace: "#808080"),
            
        new ThemeDefinition("Monokai", ThemeType.Dark,
            background: "#272822",
            secondaryBackground: "#2D2E27",
            tertiaryBackground: "#383830",
            border: "#49483E",
            foreground: "#F8F8F2",
            foregroundDim: "#75715E",
            accent: "#A6E22E",
            accentHover: "#B6F23E",
            selection: "#49483E",
            lineNumber: "#90908A",
            error: "#F92672",
            warning: "#E6DB74",
            info: "#66D9EF",
            debug: "#A6E22E",
            trace: "#75715E"),
            
        new ThemeDefinition("Dracula", ThemeType.Dark,
            background: "#282A36",
            secondaryBackground: "#21222C",
            tertiaryBackground: "#343746",
            border: "#44475A",
            foreground: "#F8F8F2",
            foregroundDim: "#6272A4",
            accent: "#BD93F9",
            accentHover: "#CDA4FA",
            selection: "#44475A",
            lineNumber: "#6272A4",
            error: "#FF5555",
            warning: "#F1FA8C",
            info: "#8BE9FD",
            debug: "#50FA7B",
            trace: "#6272A4"),
            
        new ThemeDefinition("Light", ThemeType.Light,
            background: "#FFFFFF",
            secondaryBackground: "#F5F5F5",
            tertiaryBackground: "#EBEBEB",
            border: "#CCCCCC",
            foreground: "#000000",
            foregroundDim: "#555555",
            accent: "#005A9E",
            accentHover: "#106EBE",
            selection: "#B3D7F5",
            lineNumber: "#098658",
            error: "#C50F1F",
            warning: "#9D5D00",
            info: "#005A9E",
            debug: "#1F7D1F",
            trace: "#555555"),
            
        new ThemeDefinition("High Contrast", ThemeType.Dark,
            background: "#000000",
            secondaryBackground: "#0A0A0A",
            tertiaryBackground: "#1A1A1A",
            border: "#6FC3DF",
            foreground: "#FFFFFF",
            foregroundDim: "#D4D4D4",
            accent: "#6FC3DF",
            accentHover: "#8FD3EF",
            selection: "#0E639C",
            lineNumber: "#FFFFFF",
            error: "#FF6B6B",
            warning: "#FFD93D",
            info: "#6FC3DF",
            debug: "#6BCB77",
            trace: "#D4D4D4"),
    };
    
    [ObservableProperty]
    private ThemeDefinition _currentTheme = AvailableThemes[0];
    
    [ObservableProperty]
    private int _currentThemeIndex = 0;
    
    private ThemeService()
    {
        Logger.Info("ThemeService initialized");
    }
    
    public void ApplyTheme(int index)
    {
        if (index < 0 || index >= AvailableThemes.Length) return;
        
        CurrentThemeIndex = index;
        CurrentTheme = AvailableThemes[index];
        
        Logger.Info($"Applying theme: {CurrentTheme.Name}");
        
        var resources = Application.Current.Resources;
        
        // Colors
        resources["BackgroundColor"] = ColorFromHex(CurrentTheme.Background);
        resources["SecondaryBackgroundColor"] = ColorFromHex(CurrentTheme.SecondaryBackground);
        resources["TertiaryBackgroundColor"] = ColorFromHex(CurrentTheme.TertiaryBackground);
        resources["BorderColor"] = ColorFromHex(CurrentTheme.Border);
        resources["ForegroundColor"] = ColorFromHex(CurrentTheme.Foreground);
        resources["ForegroundDimColor"] = ColorFromHex(CurrentTheme.ForegroundDim);
        resources["AccentColor"] = ColorFromHex(CurrentTheme.Accent);
        resources["AccentHoverColor"] = ColorFromHex(CurrentTheme.AccentHover);
        resources["SelectionColor"] = ColorFromHex(CurrentTheme.Selection);
        
        // Brushes - main names
        resources["BackgroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Background));
        resources["SecondaryBackgroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.SecondaryBackground));
        resources["TertiaryBackgroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.TertiaryBackground));
        resources["BorderBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Border));
        resources["ForegroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Foreground));
        resources["ForegroundDimBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.ForegroundDim));
        resources["AccentBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Accent));
        resources["AccentHoverBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.AccentHover));
        resources["SelectionBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Selection));
        resources["LineNumberBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.LineNumber));
        
        // Brushes - aliases for DarkTheme.xaml compatibility
        resources["PrimaryBackgroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Background));
        resources["PrimaryForegroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Foreground));
        resources["SecondaryForegroundBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.ForegroundDim));
        resources["HoverBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.TertiaryBackground));
        
        // Log level brushes
        resources["ErrorBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Error));
        resources["WarningBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Warning));
        resources["InfoBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Info));
        resources["DebugBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Debug));
        resources["TraceBrush"] = new SolidColorBrush(ColorFromHex(CurrentTheme.Trace));
    }
    
    public void NextTheme()
    {
        ApplyTheme((CurrentThemeIndex + 1) % AvailableThemes.Length);
    }
    
    private static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }
}

public enum ThemeType
{
    Light,
    Dark
}

public class ThemeDefinition
{
    public string Name { get; }
    public ThemeType Type { get; }
    public string Background { get; }
    public string SecondaryBackground { get; }
    public string TertiaryBackground { get; }
    public string Border { get; }
    public string Foreground { get; }
    public string ForegroundDim { get; }
    public string Accent { get; }
    public string AccentHover { get; }
    public string Selection { get; }
    public string LineNumber { get; }
    public string Error { get; }
    public string Warning { get; }
    public string Info { get; }
    public string Debug { get; }
    public string Trace { get; }
    
    public ThemeDefinition(string name, ThemeType type,
        string background, string secondaryBackground, string tertiaryBackground,
        string border, string foreground, string foregroundDim,
        string accent, string accentHover, string selection, string lineNumber,
        string error, string warning, string info, string debug, string trace)
    {
        Name = name;
        Type = type;
        Background = background;
        SecondaryBackground = secondaryBackground;
        TertiaryBackground = tertiaryBackground;
        Border = border;
        Foreground = foreground;
        ForegroundDim = foregroundDim;
        Accent = accent;
        AccentHover = accentHover;
        Selection = selection;
        LineNumber = lineNumber;
        Error = error;
        Warning = warning;
        Info = info;
        Debug = debug;
        Trace = trace;
    }
}
