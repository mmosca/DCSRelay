using DCSRelay.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DCSRelay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private MainWindowViewModel? _mainWindowViewModel;
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();
        ServiceProvider = _serviceProvider;

        // Load app configuration before creating the window so startup preferences are applied reliably.
        var startMinimized = false;
        try
        {
            var appConfig = _serviceProvider.GetService<Services.IAppConfigurationService>();
            if (appConfig != null)
            {
                appConfig.LoadAsync().GetAwaiter().GetResult();
                startMinimized = appConfig.StartMinimized;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading app configuration at startup: {ex.Message}");
        }

        // Resolve MainWindow from DI container - this will recursively resolve all dependencies
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainWindowViewModel = mainWindow.DataContext as MainWindowViewModel;
        mainWindow.Show();

        if (startMinimized)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Application is shutting down...");

        // Get main window and signal it to allow closing
        var mainWindow = _serviceProvider?.GetRequiredService<MainWindow>();
        if (mainWindow != null)
        {
            mainWindow.AllowClose();
        }

        // Save active ports before shutting down
        System.Diagnostics.Debug.WriteLine("Saving active ports before shutdown...");
        SaveActivePortsAsync();

        // Stop the relay service and dispose it
        System.Diagnostics.Debug.WriteLine("Stopping and disposing relay service...");
        DisposeRelayService();

        // Dispose of the service provider when the application exits
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
            System.Diagnostics.Debug.WriteLine("Service provider disposed.");
        }

        base.OnExit(e);
    }

    private void DisposeRelayService()
    {
        try
        {
            var relayService = _serviceProvider?.GetRequiredService<Services.IMulticastUdpRelayService>();
            if (relayService != null)
            {
                if (relayService.IsRunning)
                {
                    relayService.StopAsync().Wait(TimeSpan.FromSeconds(5));
                    System.Diagnostics.Debug.WriteLine("Multicast UDP relay service stopped.");
                }

                // Dispose the service completely
                relayService.Dispose();
                System.Diagnostics.Debug.WriteLine("Multicast UDP relay service disposed.");
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Relay service stop timed out.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping/disposing relay service: {ex.Message}");
        }
    }

    private void StopRelayServiceAsync()
    {
        try
        {
            var relayService = _serviceProvider?.GetRequiredService<Services.IMulticastUdpRelayService>();
            if (relayService?.IsRunning == true)
            {
                relayService.StopAsync().Wait();
                System.Diagnostics.Debug.WriteLine("Multicast UDP relay service stopped.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping relay service: {ex.Message}");
        }
    }

    private void SaveActivePortsAsync()
    {
        try
        {
            if (_mainWindowViewModel?.ActivePorts != null)
            {
                var portConfigService = _serviceProvider?.GetRequiredService<Services.IPortConfigurationService>();
                if (portConfigService != null)
                {
                    var activePortsList = _mainWindowViewModel.ActivePorts.ToList();
                    portConfigService.SaveActivePortsAsync(activePortsList).Wait();
                    System.Diagnostics.Debug.WriteLine($"Active ports saved on exit ({activePortsList.Count} ports).");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving ports on exit: {ex.Message}");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<Services.IPortDiscoveryService, Services.PortDiscoveryService>();
        services.AddSingleton<Services.IHardwarePortLookupService, Services.HardwarePortLookupService>();
        services.AddSingleton<Services.IMulticastUdpRelayService, Services.MulticastUdpRelayService>();
        // App configuration service for storing user preferences
        services.AddSingleton<Services.IAppConfigurationService, Services.AppConfigurationService>();
        services.AddSingleton<Services.IWindowsStartupRegistrationService, Services.WindowsStartupRegistrationService>();
        services.AddSingleton<Services.IPortConfigurationService, Services.PortConfigurationService>();
        services.AddSingleton<Services.DeviceChangeMonitorService>();

        // Register ViewModels (Singleton keeps state across app lifetime)
        services.AddSingleton<MainWindowViewModel>();

        // Register Windows/Views
        services.AddSingleton<MainWindow>();
    }
}

