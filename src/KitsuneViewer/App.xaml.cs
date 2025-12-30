using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using KitsuneViewer.Services;

namespace KitsuneViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Setup global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        
        Logger.Info("Application starting...");
        
        // Handle command line arguments (files to open)
        if (e.Args.Length > 0)
        {
            Logger.Info($"Command line args: {string.Join(", ", e.Args)}");
            
            var mainWindow = new Views.MainWindow();
            mainWindow.Show();
            
            foreach (var filePath in e.Args)
            {
                if (System.IO.File.Exists(filePath))
                {
                    Logger.Info($"Opening file from command line: {filePath}");
                    ((ViewModels.MainViewModel)mainWindow.DataContext).OpenFile(filePath);
                }
            }
        }
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Logger.Error("FATAL: Unhandled exception in AppDomain", ex);
        
        MessageBox.Show(
            $"Fatal error occurred:\n\n{ex?.Message}\n\nLog file: {Logger.GetLogFilePath()}",
            "Kitsune Viewer - Fatal Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
    
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled exception in Dispatcher", e.Exception);
        
        MessageBox.Show(
            $"An error occurred:\n\n{e.Exception.Message}\n\nLog file: {Logger.GetLogFilePath()}",
            "Kitsune Viewer - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        
        e.Handled = true; // Prevent crash, continue running
    }
    
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("Unobserved task exception", e.Exception);
        e.SetObserved(); // Prevent crash
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");
        base.OnExit(e);
    }
}
