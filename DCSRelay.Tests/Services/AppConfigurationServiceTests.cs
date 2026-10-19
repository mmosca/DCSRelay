using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DCSRelay.Services;
using Xunit;

namespace DCSRelay.Tests.Services
{
    public class AppConfigurationServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly AppConfigurationService _service;

        public AppConfigurationServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "DCSRelayTests_AppConfig_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _service = new AppConfigurationService(_testDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Fact]
        public async Task LoadAsync_FileNotFound_UsesDefaults()
        {
            // Arrange: Directory is empty

            // Act
            await _service.LoadAsync();

            // Assert
            Assert.False(_service.StartMinimized);
            Assert.False(_service.StartWithWindows);
            Assert.Equal("127.0.0.1:7778", _service.DcsBiosCommandAddress);
        }

        [Fact]
        public async Task LoadAsync_ValidFile_LoadsValues()
        {
            // Arrange
            var configPath = Path.Combine(_testDir, "app-config.json");
            var json = "{\"StartMinimized\": true, \"StartWithWindows\": true, \"DcsBiosCommandAddress\": \"10.0.0.1:1234\"}";
            await File.WriteAllTextAsync(configPath, json);

            // Act
            await _service.LoadAsync();

            // Assert
            Assert.True(_service.StartMinimized);
            Assert.True(_service.StartWithWindows);
            Assert.Equal("10.0.0.1:1234", _service.DcsBiosCommandAddress);
        }

        [Fact]
        public async Task LoadAsync_CorruptFile_HandlesExceptionAndUsesDefaults()
        {
            // Arrange
            var configPath = Path.Combine(_testDir, "app-config.json");
            await File.WriteAllTextAsync(configPath, "{ invalid json }");

            // Act
            await _service.LoadAsync();

            // Assert
            // Should still have defaults
            Assert.False(_service.StartMinimized);
            Assert.False(_service.StartWithWindows);
            Assert.Equal("127.0.0.1:7778", _service.DcsBiosCommandAddress);
        }

        [Fact]
        public async Task SaveAsync_WritesToFile()
        {
            // Arrange
            _service.StartMinimized = true;
            _service.StartWithWindows = false;
            _service.DcsBiosCommandAddress = "192.168.1.10:8888";

            // Act
            await _service.SaveAsync();

            // Assert
            var configPath = Path.Combine(_testDir, "app-config.json");
            Assert.True(File.Exists(configPath));

            var json = await File.ReadAllTextAsync(configPath);
            Assert.Contains("\"StartMinimized\": true", json);
            Assert.Contains("\"StartWithWindows\": false", json);
            Assert.Contains("\"DcsBiosCommandAddress\": \"192.168.1.10:8888\"", json);
        }
    }
}
