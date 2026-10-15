using System;
using System.Text.RegularExpressions;
using System.Windows;
using DCSRelay.Services;

namespace DCSRelay
{
    public partial class PreferencesWindow : Window
    {
        private readonly IAppConfigurationService? _configService;

        public PreferencesWindow(IAppConfigurationService? configService)
        {
            InitializeComponent();
            _configService = configService;

            // Load values from config service (if available)
            if (_configService != null)
            {
                chkStartMinimized.IsChecked = _configService.StartMinimized;
                txtDcsBiosAddress.Text = _configService.DcsBiosCommandAddress;
            }
            else
            {
                chkStartMinimized.IsChecked = false;
                txtDcsBiosAddress.Text = "127.0.0.1:7778";
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (_configService != null)
            {
                _configService.StartMinimized = chkStartMinimized.IsChecked == true;
                var value = txtDcsBiosAddress.Text?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = "127.0.0.1:7778";
                }

                // Basic validation: ip:port or ip
                if (!IsValidAddress(value))
                {
                System.Windows.MessageBox.Show(this, "Please enter a valid IP or IP:port (e.g. 127.0.0.1:7778)", "Invalid address", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _configService.DcsBiosCommandAddress = value;
                // Await normally so continuation runs on the UI thread and UI operations after await are safe
                await _configService.SaveAsync();
            }

            DialogResult = true;
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static bool IsValidAddress(string value)
        {
            // very simple validation
            var parts = value.Split(':');
            if (parts.Length == 1)
            {
                return System.Net.IPAddress.TryParse(parts[0], out _);
            }
            if (parts.Length == 2 && int.TryParse(parts[1], out var port))
            {
                return System.Net.IPAddress.TryParse(parts[0], out _) && port > 0 && port <= 65535;
            }
            return false;
        }
    }
}
