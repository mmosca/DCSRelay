using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DCSRelay.Models;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service for persisting port configuration to JSON files in LocalAppData.
    /// </summary>
    public class PortConfigurationService : IPortConfigurationService
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private const string ConfigFileName = "port-config.json";
        private const string AppDataFolderName = "DCSRelay";

        public PortConfigurationService()
        {
            // Get LocalAppData path (e.g., C:\Users\<username>\AppData\Local)
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _configDirectory = Path.Combine(appDataPath, AppDataFolderName);
            _configFilePath = Path.Combine(_configDirectory, ConfigFileName);
        }

        /// <summary>
        /// Loads the previously saved active ports from the configuration file.
        /// </summary>
        public async Task<List<string>> LoadActivePortsAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Configuration file not found at {_configFilePath}");
                    return new List<string>();
                }

                var jsonContent = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<PortConfiguration>(jsonContent);

                return config?.ActivePorts ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading port configuration: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Saves the current list of active ports to the configuration file.
        /// </summary>
        public async Task SaveActivePortsAsync(List<HardwareDeviceInfo> activePorts)
        {
            try
            {
                // Create the directory if it doesn't exist
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                var currentActivePortIds = activePorts
                    .Select(p => p.DeviceID)
                    .Where(deviceId => !string.IsNullOrWhiteSpace(deviceId))
                    .ToList();

                var previouslySavedPortIds = await LoadActivePortsAsync().ConfigureAwait(false);
                foreach (var previouslySavedPortId in previouslySavedPortIds)
                {
                    if (!currentActivePortIds.Contains(previouslySavedPortId, StringComparer.OrdinalIgnoreCase))
                    {
                        currentActivePortIds.Add(previouslySavedPortId);
                    }
                }

                var config = new PortConfiguration
                {
                    ActivePorts = currentActivePortIds,
                    LastSaved = DateTime.Now
                };

                var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, jsonContent);

                System.Diagnostics.Debug.WriteLine($"Port configuration saved to {_configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving port configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Internal class for JSON serialization structure.
        /// </summary>
        private class PortConfiguration
        {
            public List<string> ActivePorts { get; set; } = new List<string>();
            public DateTime LastSaved { get; set; }
        }
    }
}
