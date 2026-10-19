using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using DCSRelay.Services;
using DCSRelay.Models; // Needed for DependencyObject if we were doing full MVVM, but for now, I'll use simplified implementation.

namespace DCSRelay.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        // Basic Command Implementation (simplified for this example)
        public ICommand MovePortLeftCommand { get; }
        public ICommand MovePortRightCommand { get; }
        private IPortDiscoveryService _portDiscoveryService;
        private IMulticastUdpRelayService _relayService;
        private IPortConfigurationService _portConfigurationService;

        private long _bytesReceivedOnMulticast;
        public long BytesReceivedOnMulticast
        {
            get { return _bytesReceivedOnMulticast; }
            set
            {
                if (_bytesReceivedOnMulticast != value)
                {
                    _bytesReceivedOnMulticast = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _relayServiceStatus;
        public string RelayServiceStatus
        {
            get { return _relayServiceStatus; }
            set
            {
                if (_relayServiceStatus != value)
                {
                    _relayServiceStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public IPortDiscoveryService PortDiscoveryService
        {
            get { return this._portDiscoveryService; }
            set
            {
                this._portDiscoveryService = value;
                OnPropertyChanged();
            }
        }

        public Task InitializationTask { get; private set; }
        public MainWindowViewModel(IPortDiscoveryService portDiscoveryService, IMulticastUdpRelayService relayService, IPortConfigurationService portConfigurationService)
        {
            this.AvailablePorts = new ObservableCollection<HardwareDeviceInfo>();
            this.ActivePorts = new ObservableCollection<HardwareDeviceInfo>();
            this.SelectedAvailablePorts = new ObservableCollection<HardwareDeviceInfo>();
            this.SelectedActivePorts = new ObservableCollection<HardwareDeviceInfo>();
            MovePortLeftCommand = new RelayCommand(MovePortLeft);
            MovePortRightCommand = new RelayCommand(MovePortRight);
            this._relayService = relayService;
            this._portConfigurationService = portConfigurationService;

            // Initialize status
            _bytesReceivedOnMulticast = 0;
            _relayServiceStatus = "Relay: Initializing...";

            // Wire up relay service events
            this._relayService.DataRelayed += OnDataRelayed;
            this._relayService.ErrorOccurred += OnRelayServiceError;

            // Capture the initialization task
            InitializationTask = InitializeAndPopulateAsync(portDiscoveryService);
        }

        private async Task InitializeAndPopulateAsync(IPortDiscoveryService portDiscoveryService)
        {
            try 
            {
                await InitializeAsync();
                // Trigger port population and wait for it to finish
                await PopulatePortsAsync(portDiscoveryService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization failed: {ex.Message}");
            }
        }

        private async Task InitializeAsync()
        {
            // Load previously saved active ports
            await LoadSavedPortsAsync();

            // Start the relay service
            await InitializeRelayServiceAsync();
        }

        private async Task LoadSavedPortsAsync()
        {
            try
            {
                var savedDeviceIds = await _portConfigurationService.LoadActivePortsAsync();
                if (savedDeviceIds.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded {savedDeviceIds.Count} saved ports from configuration.");
                    // Store for later matching after ports are discovered
                    _savedDeviceIds = new List<string>(savedDeviceIds);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading saved ports: {ex.Message}");
            }
        }

        private List<string>? _savedDeviceIds;

        private async Task InitializeRelayServiceAsync()
        {
            try
            {
                if (!_relayService.IsRunning)
                {
                    await _relayService.StartAsync();
                    RelayServiceStatus = "Relay: Running";
                    System.Diagnostics.Debug.WriteLine("Multicast UDP relay service started.");
                }
            }
            catch (Exception ex)
            {
                RelayServiceStatus = $"Relay: Error - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Failed to start relay service: {ex.Message}");
            }
        }

        private void OnDataRelayed(object? sender, RelayEventArgs e)
        {
            Action updateAction = () => { BytesReceivedOnMulticast += e.BytesRelayed; };
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        private void OnRelayServiceError(object? sender, System.IO.ErrorEventArgs e)
        {
            Action updateAction = () =>
            {
                RelayServiceStatus = $"Relay: Error";
                System.Diagnostics.Debug.WriteLine($"Relay service error: {e.GetException()?.Message}");
            };
            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        // Data Collections
        public ObservableCollection<HardwareDeviceInfo> AvailablePorts { get; set; }
        public ObservableCollection<HardwareDeviceInfo> ActivePorts { get; set; }

        // Selected Items Collections
        private ObservableCollection<HardwareDeviceInfo> _selectedAvailablePorts;
        public ObservableCollection<HardwareDeviceInfo> SelectedAvailablePorts
        {
            get { return _selectedAvailablePorts; }
            set
            {
                if (_selectedAvailablePorts != value)
                {
                    _selectedAvailablePorts = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<HardwareDeviceInfo> _selectedActivePorts;
        public ObservableCollection<HardwareDeviceInfo> SelectedActivePorts
        {
            get { return _selectedActivePorts; }
            set
            {
                if (_selectedActivePorts != value)
                {
                    _selectedActivePorts = value;
                    OnPropertyChanged();
                }
            }
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Command Implementations ---
        private void MovePortLeft(object parameter)
        {
            var portsToMove = SelectedActivePorts.ToList();
            foreach (var portToMove in portsToMove)
            {
                ActivePorts.Remove(portToMove);
                AvailablePorts.Add(portToMove);
                SelectedActivePorts.Remove(portToMove);
            }

            SortPortLists();

            // Update the relay service with the new active ports list
            UpdateRelayService();
        }

        private void MovePortRight(object parameter)
        {
            var portsToMove = SelectedAvailablePorts.ToList();
            foreach (var portToMove in portsToMove)
            {
                AvailablePorts.Remove(portToMove);
                ActivePorts.Add(portToMove);
                SelectedAvailablePorts.Remove(portToMove);
            }

            SortPortLists();

            // Update the relay service with the new active ports list
            UpdateRelayService();
        }

        private void UpdateRelayService()
        {
            var activePortsList = ActivePorts.ToList();
            _relayService.UpdateActivePorts(activePortsList);
            System.Diagnostics.Debug.WriteLine($"Relay service updated with {activePortsList.Count} active ports.");
        }

        public bool IsSavedActiveDevice(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId) || _savedDeviceIds == null || _savedDeviceIds.Count == 0)
            {
                return false;
            }

            return _savedDeviceIds.Contains(deviceId, StringComparer.OrdinalIgnoreCase);
        }

        public void NotifyActivePortsChanged()
        {
            UpdateRelayService();
        }

        internal void SortAvailablePorts()
        {
            SortPortCollection(AvailablePorts);
        }

        private void SortPortLists()
        {
            SortAvailablePorts();
            SortPortCollection(ActivePorts);
        }

        private static void SortPortCollection(ObservableCollection<HardwareDeviceInfo> ports)
        {
            var sortedPorts = ports.OrderBy(port => port.PortName, PortNameComparer.Instance).ToList();
            for (var index = 0; index < sortedPorts.Count; index++)
            {
                var currentIndex = ports.IndexOf(sortedPorts[index]);
                if (currentIndex != index)
                {
                    ports.Move(currentIndex, index);
                }
            }
        }

        private async Task PopulatePortsAsync(IPortDiscoveryService portDiscoveryService)
        {
            this._portDiscoveryService = portDiscoveryService;
            var ports = await portDiscoveryService.DiscoverPortsAsync();

            Action updateAction = () =>
            {
                AvailablePorts.Clear();
                foreach (var port in ports)
                {
                    AvailablePorts.Add(port);
                }

                // Restore previously saved active ports if available
                if (_savedDeviceIds != null && _savedDeviceIds.Count > 0)
                {
                    var savedPorts = new List<HardwareDeviceInfo>();
                    foreach (var savedDeviceId in _savedDeviceIds)
                    {
                        var matchingPort = AvailablePorts.FirstOrDefault(p => p.DeviceID == savedDeviceId);
                        if (matchingPort != null)
                        {
                            savedPorts.Add(matchingPort);
                        }
                    }

                    // Move saved ports to active list
                    foreach (var port in savedPorts)
                    {
                        AvailablePorts.Remove(port);
                        ActivePorts.Add(port);
                    }

                    System.Diagnostics.Debug.WriteLine($"Restored {savedPorts.Count} previously active ports.");
                    UpdateRelayService();
                }

                SortPortLists();
            };

            if (System.Windows.Application.Current?.Dispatcher != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }
    }
}