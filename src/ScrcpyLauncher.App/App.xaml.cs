using System.Windows;
using ScrcpyLauncher.App.Infrastructure;

namespace ScrcpyLauncher.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        StartupLogger.ResetSession();
        StartupLogger.Write("App.OnStartup entered.");
        StartupLogger.Write($"BaseDirectory={AppContext.BaseDirectory}");
        StartupLogger.Write($"CurrentDirectory={Environment.CurrentDirectory}");
        StartupLogger.Write($"LogFile={StartupLogger.LogFilePath}");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            base.OnStartup(e);

            var window = new MainWindow();
            MainWindow = window;
            StartupLogger.Write("MainWindow created.");
            window.Show();
            StartupLogger.Write("MainWindow shown.");
        }
        catch (Exception ex)
        {
            StartupLogger.WriteException("Fatal startup error", ex);
            MessageBox.Show(
                "The scrcpy launcher failed to start.\n\nLog: " + StartupLogger.LogFilePath,
                "scrcpy Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StartupLogger.Write($"App.OnExit code={e.ApplicationExitCode}");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        StartupLogger.WriteException("DispatcherUnhandledException", e.Exception);
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            StartupLogger.WriteException("AppDomain.CurrentDomain.UnhandledException", exception);
        }
        else
        {
            StartupLogger.Write("AppDomain.CurrentDomain.UnhandledException with non-Exception payload.");
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupLogger.WriteException("TaskScheduler.UnobservedTaskException", e.Exception);
    }
}
