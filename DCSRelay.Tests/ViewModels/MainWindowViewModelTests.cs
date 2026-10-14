using DCSRelay.Models;
using DCSRelay.Services;
using DCSRelay.ViewModels;

namespace DCSRelay.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_starts_port_discovery_and_loads_saved_configuration()
    {
        var portDiscovery = new FakePortDiscoveryService();
        var portConfiguration = new FakePortConfigurationService();

        _ = CreateViewModel(portDiscovery, new FakeRelayService(), portConfiguration);

        Assert.Equal(1, portDiscovery.DiscoverPortsCallCount);
        Assert.Equal(1, portConfiguration.LoadActivePortsCallCount);
    }

    [Fact]
    public void MovePortRightCommand_moves_selected_ports_and_sorts_both_lists()
    {
        var relayService = new FakeRelayService();
        var viewModel = CreateViewModel(relayService: relayService);
        var com10 = CreatePort("COM10");
        var com1 = CreatePort("COM1");
        var com2 = CreatePort("COM2");
        viewModel.AvailablePorts.Add(com10);
        viewModel.AvailablePorts.Add(com1);
        viewModel.AvailablePorts.Add(com2);
        viewModel.SelectedAvailablePorts.Add(com10);
        viewModel.SelectedAvailablePorts.Add(com2);

        viewModel.MovePortRightCommand.Execute(new object());

        Assert.Equal(new[] { "COM2", "COM10" }, viewModel.ActivePorts.Select(port => port.PortName));
        Assert.Equal(new[] { "COM1" }, viewModel.AvailablePorts.Select(port => port.PortName));
        Assert.Empty(viewModel.SelectedAvailablePorts);
        Assert.Equal(new[] { "COM2", "COM10" }, relayService.LastUpdatedPorts.Select(port => port.PortName));
        Assert.Equal(1, relayService.UpdateActivePortsCallCount);
    }

    [Fact]
    public void MovePortLeftCommand_moves_selected_ports_and_sorts_both_lists()
    {
        var relayService = new FakeRelayService();
        var viewModel = CreateViewModel(relayService: relayService);
        var com10 = CreatePort("COM10");
        var com3 = CreatePort("COM3");
        var com2 = CreatePort("COM2");
        viewModel.ActivePorts.Add(com10);
        viewModel.ActivePorts.Add(com3);
        viewModel.AvailablePorts.Add(com2);
        viewModel.SelectedActivePorts.Add(com10);

        viewModel.MovePortLeftCommand.Execute(new object());

        Assert.Equal(new[] { "COM3" }, viewModel.ActivePorts.Select(port => port.PortName));
        Assert.Equal(new[] { "COM2", "COM10" }, viewModel.AvailablePorts.Select(port => port.PortName));
        Assert.Empty(viewModel.SelectedActivePorts);
        Assert.Equal(new[] { "COM3" }, relayService.LastUpdatedPorts.Select(port => port.PortName));
        Assert.Equal(1, relayService.UpdateActivePortsCallCount);
    }

    [Fact]
    public void Property_setters_raise_notifications_only_when_values_change()
    {
        var viewModel = CreateViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.BytesReceivedOnMulticast = 42;
        viewModel.BytesReceivedOnMulticast = 42;
        viewModel.RelayServiceStatus = "Relay: Running";
        viewModel.RelayServiceStatus = "Relay: Running";

        Assert.Equal(new[] { "BytesReceivedOnMulticast", "RelayServiceStatus" }, changedProperties);
    }

    private static MainWindowViewModel CreateViewModel(
        FakePortDiscoveryService? portDiscovery = null,
        FakeRelayService? relayService = null,
        FakePortConfigurationService? portConfiguration = null)
    {
        return new MainWindowViewModel(
            portDiscovery ?? new FakePortDiscoveryService(),
            relayService ?? new FakeRelayService(),
            portConfiguration ?? new FakePortConfigurationService());
    }

    private static HardwareDeviceInfo CreatePort(string portName) => new() { PortName = portName };

    private sealed class FakePortDiscoveryService : IPortDiscoveryService
    {
        private readonly TaskCompletionSource<List<HardwareDeviceInfo>> _discovery = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int DiscoverPortsCallCount { get; private set; }

        public Task<List<HardwareDeviceInfo>> DiscoverPortsAsync()
        {
            DiscoverPortsCallCount++;
            return _discovery.Task;
        }
    }

    private sealed class FakePortConfigurationService : IPortConfigurationService
    {
        private readonly TaskCompletionSource<List<string>> _savedPorts = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public int LoadActivePortsCallCount { get; private set; }

        public Task<List<string>> LoadActivePortsAsync()
        {
            LoadActivePortsCallCount++;
            return _savedPorts.Task;
        }

        public Task SaveActivePortsAsync(List<HardwareDeviceInfo> activePorts) => Task.CompletedTask;
    }

    private sealed class FakeRelayService : IMulticastUdpRelayService
    {
        public bool IsRunning => false;
        public List<HardwareDeviceInfo> LastUpdatedPorts { get; private set; } = [];
        public int UpdateActivePortsCallCount { get; private set; }

        public event EventHandler<RelayEventArgs>? DataRelayed
        {
            add { }
            remove { }
        }

        public event EventHandler<System.IO.ErrorEventArgs>? ErrorOccurred
        {
            add { }
            remove { }
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
        public void UpdateActivePorts(List<HardwareDeviceInfo> ports)
        {
            UpdateActivePortsCallCount++;
            LastUpdatedPorts = ports;
        }

        public void AddPort(HardwareDeviceInfo port) { }
        public void RemovePort(HardwareDeviceInfo port) { }
        public int GetActivePortCount() => LastUpdatedPorts.Count;
        public void Dispose() { }
    }
}
