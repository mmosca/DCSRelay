using System.Collections.Generic;
using System.Threading.Tasks;
using DCSRelay.Models;
using DCSRelay.Services;
using Xunit;

namespace DCSRelay.Tests.Services
{
    public class PortDiscoveryServiceTests
    {
        private class FakeHardwareLookupService : IHardwarePortLookupService
        {
            public Dictionary<string, HardwareDeviceInfo> MockDevices { get; } = new Dictionary<string, HardwareDeviceInfo>();

            public Task<HardwareDeviceInfo> GetHardwareDetailsAsync(string portName)
            {
                if (MockDevices.TryGetValue(portName, out var info))
                {
                    return Task.FromResult(info);
                }
                return Task.FromResult<HardwareDeviceInfo>(null);
            }
        }

        private class FakePortProvider : ISerialPortProvider
        {
            public string[] PortNames { get; set; } = new string[0];
            public string[] GetPortNames() => PortNames;
        }

        [Fact]
        public async Task DiscoverPortsAsync_NoPortsAvailable_ReturnsEmptyList()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService();
            var portSvc = new FakePortProvider { PortNames = new string[0] };
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.DiscoverPortsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiscoverPortsAsync_PortsAvailableButNoHardwareMatch_ReturnsEmptyList()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService(); // No devices mapped
            var portSvc = new FakePortProvider { PortNames = new[] { "COM1", "COM2" } };
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.DiscoverPortsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiscoverPortsAsync_MixedHardwareMatch_ReturnsOnlyMatchingPorts()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService();
            hardwareSvc.MockDevices["COM1"] = new HardwareDeviceInfo { PortName = "COM1", DeviceID = "ID1" };
            // COM2 is left unmatched

            var portSvc = new FakePortProvider { PortNames = new[] { "COM1", "COM2" } };
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.DiscoverPortsAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("COM1", result[0].PortName);
        }

        [Fact]
        public async Task DiscoverPortsAsync_MultiplePorts_ReturnsSortedList()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService();
            hardwareSvc.MockDevices["COM10"] = new HardwareDeviceInfo { PortName = "COM10", DeviceID = "ID10" };
            hardwareSvc.MockDevices["COM1"] = new HardwareDeviceInfo { PortName = "COM1", DeviceID = "ID1" };
            hardwareSvc.MockDevices["COM2"] = new HardwareDeviceInfo { PortName = "COM2", DeviceID = "ID2" };

            var portSvc = new FakePortProvider { PortNames = new[] { "COM10", "COM1", "COM2" } };
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.DiscoverPortsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            // Natural sort order should be COM1, COM2, COM10
            Assert.Equal("COM1", result[0].PortName);
            Assert.Equal("COM2", result[1].PortName);
            Assert.Equal("COM10", result[2].PortName);
        }

        [Fact]
        public async Task GetPortHardwareDetailsAsync_DelegatesToLookupService()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService();
            var expected = new HardwareDeviceInfo { PortName = "COM3", DeviceID = "ID3" };
            hardwareSvc.MockDevices["COM3"] = expected;

            var portSvc = new FakePortProvider();
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.GetPortHardwareDetailsAsync("COM3");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetPortHardwareDetailsAsync_PortNotFound_ReturnsNull()
        {
            // Arrange
            var hardwareSvc = new FakeHardwareLookupService();
            var portSvc = new FakePortProvider();
            var service = new PortDiscoveryService(hardwareSvc, portSvc);

            // Act
            var result = await service.GetPortHardwareDetailsAsync("NON_EXISTENT");

            // Assert
            Assert.Null(result);
        }
    }
}
