using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DCSRelay.Services
{
    public interface IAppConfigurationService
    {
        bool StartMinimized { get; set; }
        bool StartWithWindows { get; set; }
        string DcsBiosCommandAddress { get; set; }
        Task LoadAsync();
        Task SaveAsync();
    }

    public class AppConfigurationService : IAppConfigurationService
    {
        private const string AppDataFolderName = "DCSRelay";
        private const string ConfigFileName = "app-config.json";

        private readonly string _configDirectory;
        private readonly string _configFilePath;

        public bool StartMinimized { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public string DcsBiosCommandAddress { get; set; } = "127.0.0.1:7778";

        public AppConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _configDirectory = Path.Combine(appDataPath, AppDataFolderName);
            _configFilePath = Path.Combine(_configDirectory, ConfigFileName);

            // Kick off async load in background to avoid blocking constructor / UI thread
            try
            {
                _ = LoadAsync();
            }
            catch
            {
                // Ignore load failures here; defaults will be used
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(_configFilePath).ConfigureAwait(false);
                var cfg = JsonSerializer.Deserialize<ConfigModel>(json);
                if (cfg != null)
                {
                    StartMinimized = cfg.StartMinimized;
                    StartWithWindows = cfg.StartWithWindows;
                    if (!string.IsNullOrWhiteSpace(cfg.DcsBiosCommandAddress))
                    {
                        DcsBiosCommandAddress = cfg.DcsBiosCommandAddress;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading app configuration: {ex.Message}");
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }

                var cfg = new ConfigModel
                {
                    StartMinimized = StartMinimized,
                    StartWithWindows = StartWithWindows,
                    DcsBiosCommandAddress = DcsBiosCommandAddress
                };

                var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_configFilePath, json).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Saved app configuration to {_configFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving app configuration: {ex.Message}");
            }
        }

        private class ConfigModel
        {
            public bool StartMinimized { get; set; }
            public bool StartWithWindows { get; set; }
            public string? DcsBiosCommandAddress { get; set; }
        }
    }
}
