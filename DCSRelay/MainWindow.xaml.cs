using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using DCSRelay.ViewModels;
using DCSRelay.Services;

namespace DCSRelay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SystemTrayService? _systemTrayService;
    private DeviceChangeMonitorService? _deviceChangeMonitorService;
    private MainWindowViewModel? _viewModel;
    private bool _isClosing = false;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        // Set the ViewModel as the DataContext for data binding
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Initialize system tray after window is initialized
        _systemTrayService = new SystemTrayService();
        _systemTrayService.Initialize(this);
        System.Diagnostics.Debug.WriteLine("System tray service initialized.");

        // Initialize device change monitor to refresh ports on device changes
        if (_viewModel != null)
        {
            _deviceChangeMonitorService = new DeviceChangeMonitorService();
            _deviceChangeMonitorService.Initialize(_viewModel);
            System.Diagnostics.Debug.WriteLine("Device change monitor service initialized.");
        }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // If not explicitly closing the application, minimize to tray instead
        if (!_isClosing)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            System.Diagnostics.Debug.WriteLine("MainWindow closing event intercepted - minimizing to tray.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("MainWindow closing - application is shutting down.");
        }
    }

    private void MainWindow_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            ShowInTaskbar = false;
            System.Diagnostics.Debug.WriteLine("Window minimized to tray.");
        }
    }

    /// <summary>
    /// Allows explicit application shutdown to close the window before exit cleanup runs.
    /// </summary>
    public void PrepareForApplicationShutdown()
    {
        _isClosing = true;
    }

    /// <summary>
    /// Allows Application OnExit to signal that a real close is happening.
    /// </summary>
    public void AllowClose()
    {
        _isClosing = true;
        _deviceChangeMonitorService?.Dispose();
        _systemTrayService?.Dispose();
    }

    private void OnAvailableListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedAvailablePorts.Clear();
            foreach (var item in availableList.SelectedItems)
            {
                viewModel.SelectedAvailablePorts.Add((dynamic)item);
            }
        }
    }

    private void OnActiveListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedActivePorts.Clear();
            foreach (var item in activeList.SelectedItems)
            {
                viewModel.SelectedActivePorts.Add((dynamic)item);
            }
        }
    }

        private void OnFileExitClicked(object sender, RoutedEventArgs e)
    {
        // Prepare for shutdown then request application exit
        PrepareForApplicationShutdown();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnEditPreferencesClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var svc = App.ServiceProvider?.GetService(typeof(Services.IAppConfigurationService)) as Services.IAppConfigurationService;
            var prefs = new PreferencesWindow(svc);
            prefs.Owner = this;
            prefs.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening preferences: {ex.Message}");
        }
    }

    private void OnHelpAboutClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly() ?? System.Reflection.Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version?.ToString() ?? "?";
            var fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).FileVersion ?? "?";
            System.Windows.MessageBox.Show(this, $"DCS Relay\nVersion: {version}\nFile Version: {fileVersion}", "About DCS Relay", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing About dialog: {ex.Message}");
        }
    }
}