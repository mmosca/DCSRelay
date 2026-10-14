using System;
using System.Management;
using System.Windows;
using DCSRelay.ViewModels;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service for monitoring device changes and refreshing serial ports
    /// when USB/Serial devices are plugged in or unplugged.
    /// </summary>
    public class DeviceChangeMonitorService : IDisposable
    {
        private ManagementEventWatcher? _watcher;
        private MainWindowViewModel? _viewModel;
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Initializes the device change monitor with the view model to update.
        /// </summary>
        public void Initialize(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            try
            {
                // Query for COM port device arrivals and removals
                // This monitors for any Plug and Play device changes
                var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 1 OR EventType = 2");

                _watcher = new ManagementEventWatcher(query)
                {
                    Options =
                    {
                        Timeout = TimeSpan.FromSeconds(10)
                    }
                };

                // Subscribe to device change events
                _watcher.EventArrived += OnDeviceChanged;
                _watcher.Stopped += OnEventingStopped;

                // Start watching
                _watcher.Start();

                System.Diagnostics.Debug.WriteLine("Device change monitor started.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting device change monitor: {ex.Message}");
            }
        }

        private void OnDeviceChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                // Debounce: only refresh if enough time has passed since last refresh
                var now = DateTime.Now;
                if (now - _lastRefreshTime < _debounceDelay)
                {
                    return;
                }

                _lastRefreshTime = now;

                System.Diagnostics.Debug.WriteLine("Device change detected - refreshing serial ports.");

                // Refresh ports on the UI thread
                Application.Current?.Dispatcher?.Invoke(async () =>
                {
                    if (_viewModel != null)
                    {
                        await RefreshPortsAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling device change: {ex.Message}");
            }
        }

        private void OnEventingStopped(object sender, StoppedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Device change monitor stopped.");
        }

        private async System.Threading.Tasks.Task RefreshPortsAsync()
        {
            try
            {
                if (_viewModel?.PortDiscoveryService != null)
                {
                    System.Diagnostics.Debug.WriteLine("Refreshing port list...");
                    var ports = await _viewModel.PortDiscoveryService.DiscoverPortsAsync();

                    // Update available ports keeping previously selected items
                    var selectedPorts = new System.Collections.Generic.List<Models.HardwareDeviceInfo>(_viewModel.SelectedAvailablePorts);

                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        // Find ports that are no longer available
                        var portsToRemove = new System.Collections.Generic.List<Models.HardwareDeviceInfo>();
                        foreach (var availablePort in _viewModel.AvailablePorts)
                        {
                            if (!ports.Any(p => p.PortName == availablePort.PortName))
                            {
                                portsToRemove.Add(availablePort);
                            }
                        }

                        // Remove ports that are no longer available
                        foreach (var port in portsToRemove)
                        {
                            _viewModel.AvailablePorts.Remove(port);
                            System.Diagnostics.Debug.WriteLine($"Removed unavailable port: {port.PortName}");
                        }

                        // Add new ports
                        foreach (var port in ports)
                        {
                            if (!_viewModel.AvailablePorts.Any(p => p.PortName == port.PortName) &&
                                                            !_viewModel.ActivePorts.Any(p => p.PortName == port.PortName))
                            {
                                _viewModel.AvailablePorts.Add(port);
                                System.Diagnostics.Debug.WriteLine($"Added new port: {port.PortName}");
                            }
                        }

                        _viewModel.SortAvailablePorts();
                        System.Diagnostics.Debug.WriteLine($"Port list refreshed. Total available: {_viewModel.AvailablePorts.Count}");
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing ports: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher.EventArrived -= OnDeviceChanged;
                _watcher.Stopped -= OnEventingStopped;
                _watcher.Dispose();
                System.Diagnostics.Debug.WriteLine("Device change monitor disposed.");
            }
        }
    }
}
