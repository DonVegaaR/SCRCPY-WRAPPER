using System.Windows;
using System.Windows.Input;
using ScrcpyLauncher.App.Infrastructure;
using ScrcpyLauncher.App.ViewModels;
using ScrcpyLauncher.Infrastructure;

namespace ScrcpyLauncher.App;

public partial class MainWindow : Window
{
    private static readonly GridLength ExpandedSidebarWidth = new(112);
    private static readonly GridLength CollapsedSidebarWidth = new(0);

    private TextViewerWindow? commandWindow;
    private TextViewerWindow? logWindow;
    private bool isSidebarCollapsed;

    public MainWindow()
    {
        StartupLogger.Write("MainWindow constructor entered.");
        InitializeComponent();

        DataContext = new MainWindowViewModel(
            new JsonLaunchProfileStore(),
            new ScrcpyBinaryLocator(),
            new AdbService(),
            new ScrcpyProcessService(),
            Dispatcher);
        StartupLogger.Write("MainWindow DataContext assigned.");

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartupLogger.Write("MainWindow Loaded event.");
        if (DataContext is MainWindowViewModel viewModel)
        {
            try
            {
                await viewModel.InitializeAsync();
                StartupLogger.Write("ViewModel initialization completed.");
            }
            catch (Exception ex)
            {
                StartupLogger.WriteException("ViewModel initialization failed", ex);
                throw;
            }
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        StartupLogger.Write("MainWindow Closed event.");
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.DisposeAsync();
            StartupLogger.Write("ViewModel disposed.");
        }
    }

    private void OnViewCommandClick(object sender, RoutedEventArgs e)
    {
        OpenCommandWindow();
    }

    private void OnViewLogsClick(object sender, RoutedEventArgs e)
    {
        OpenLogWindow();
    }

    private void OpenCommandWindow()
    {
        if (commandWindow is null || !commandWindow.IsLoaded)
        {
            commandWindow = new TextViewerWindow(DataContext!, "Command Preview", "CommandPreview")
            {
                Owner = this,
            };
            commandWindow.Closed += (_, _) => commandWindow = null;
            commandWindow.Show();
            return;
        }

        commandWindow.Activate();
    }

    private void OpenLogWindow()
    {
        if (logWindow is null || !logWindow.IsLoaded)
        {
            logWindow = new TextViewerWindow(DataContext!, "Launcher Logs", "LogText")
            {
                Owner = this,
            };
            logWindow.Closed += (_, _) => logWindow = null;
            logWindow.Show();
            return;
        }

        logWindow.Activate();
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimizeWindowClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeWindowClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void OnCloseWindowClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        isSidebarCollapsed = !isSidebarCollapsed;
        SidebarColumn.Width = isSidebarCollapsed
            ? CollapsedSidebarWidth
            : ExpandedSidebarWidth;
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
