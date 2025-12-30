using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using KitsuneViewer.Models;

namespace KitsuneViewer.Converters;

/// <summary>
/// Converts count to Visibility (Visible when count is 0, Collapsed otherwise)
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to TextWrapping
/// </summary>
public class BoolToTextWrappingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool wrap)
        {
            return wrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
        return TextWrapping.NoWrap;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to ScrollBarVisibility (Disabled when true/wrap, Auto when false/nowrap)
/// </summary>
public class BoolToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool wrap)
        {
            return wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }
        return ScrollBarVisibility.Auto;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to "On"/"Off" string
/// </summary>
public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? "On" : "Off";
        }
        return "Off";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return !b;
        }
        return false;
    }
}

/// <summary>
/// Converts LogLevel to appropriate brush from resources
/// </summary>
public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            var key = level switch
            {
                LogLevel.Error => "ErrorBrush",
                LogLevel.Warning => "WarningBrush",
                LogLevel.Debug => "DebugBrush",
                LogLevel.Trace => "TraceBrush",
                _ => "ForegroundBrush"
            };
            
            return Application.Current.Resources[key] as SolidColorBrush 
                ?? new SolidColorBrush(Colors.White);
        }
        return Application.Current.Resources["ForegroundBrush"] as SolidColorBrush 
            ?? new SolidColorBrush(Colors.White);
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
