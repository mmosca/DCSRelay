using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCSRelay.Models;
using DCSRelay.Services;
using Xunit;

namespace DCSRelay.Tests.Services
{
    public class PortConfigurationServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly PortConfigurationService _service;

        public PortConfigurationServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "DCSRelayTests_PortConfig_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _service = new PortConfigurationService(_testDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Fact]
        public async Task LoadActivePortsAsync_FileNotFound_ReturnsEmptyList()
        {
            // Act
            var result = await _service.LoadActivePortsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task LoadActivePortsAsync_ValidFile_ReturnsPorts()
        {
            // Arrange
            var configPath = Path.Combine(_testDir, "port-config.json");
            var json = "{\"ActivePorts\": [\"DEV_ID_1\", \"DEV_ID_2\"], \"LastSaved\": \"2023-01-01\"}";
            await File.WriteAllTextAsync(configPath, json);

            // Act
            var result = await _service.LoadActivePortsAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("DEV_ID_1", result);
            Assert.Contains("DEV_ID_2", result);
        }

        [Fact]
        public async Task SaveActivePortsAsync_WritesToFile()
        {
            // Arrange
            var activePorts = new List<HardwareDeviceInfo>
            {
                new HardwareDeviceInfo { DeviceID = "DEV_A", PortName = "COM1" },
                new HardwareDeviceInfo { DeviceID = "DEV_B", PortName = "COM2" }
            };

            // Act
            await _service.SaveActivePortsAsync(activePorts);

            // Assert
            var configPath = Path.Combine(_testDir, "port-config.json");
            Assert.True(File.Exists(configPath));

            var json = await File.ReadAllTextAsync(configPath);
            Assert.Contains("DEV_A", json);
            Assert.Contains("DEV_B", json);
        }

        [Fact]
        public async Task SaveActivePortsAsync_MergesWithExistingPorts()
        {
            // Arrange
            var configPath = Path.Combine(_testDir, "port-config.json");
            var json = "{\"ActivePorts\": [\"OLD_ID\"], \"LastSaved\": \"2023-01-01\"}";
            await File.WriteAllTextAsync(configPath, json);

            var newPorts = new List<HardwareDeviceInfo>
            {
                new HardwareDeviceInfo { DeviceID = "NEW_ID", PortName = "COM3" }
            };

            // Act
            await _service.SaveActivePortsAsync(newPorts);

            // Assert
            var result = await _service.LoadActivePortsAsync();
            Assert.Equal(2, result.Count);
            Assert.Contains("OLD_ID", result);
            Assert.Contains("NEW_ID", result);
        }
    }
}
