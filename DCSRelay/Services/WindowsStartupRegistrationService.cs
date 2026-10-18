using System;
using System.Reflection;
using Microsoft.Win32;

namespace DCSRelay.Services
{
    public interface IWindowsStartupRegistrationService
    {
        bool IsEnabled();
        void SetEnabled(bool enabled);
    }

    public class WindowsStartupRegistrationService : IWindowsStartupRegistrationService
    {
        private const string RunRegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string StartupValueName = "DCSRelay";

        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
                var value = key?.GetValue(StartupValueName) as string;
                return !string.IsNullOrWhiteSpace(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading startup registration: {ex.Message}");
                return false;
            }
        }

        public void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true)
                    ?? Registry.CurrentUser.CreateSubKey(RunRegistryPath);

                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    var executablePath = Assembly.GetEntryAssembly()?.Location;
                    if (!string.IsNullOrWhiteSpace(executablePath))
                    {
                        key.SetValue(StartupValueName, $"\"{executablePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(StartupValueName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating startup registration: {ex.Message}");
            }
        }
    }
}
