using Microsoft.Win32;
using System;
using Windows.ApplicationModel;

namespace DCSRelay.Services
{
    public interface IWindowsStartupRegistrationService
    {
        bool IsWindowsStoreApp { get; }
        bool IsEnabled();
        void SetEnabled(bool enabled);
    }

    public class WindowsStartupRegistrationService : IWindowsStartupRegistrationService
    {
        private const string RunRegistryPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string StartupValueName = "DCSRelay";

        public bool IsEnabled()
        {
            if(this.IsWindowsStoreApp)
            {
                // Windows Store apps cannot register for startup in the same way as traditional desktop apps.
                return false;
            }

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
                    var folder = System.AppContext.BaseDirectory;
                    var executablePath = System.IO.Path.Combine(folder, "DCSRelay.exe");
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

        public bool IsWindowsStoreApp
        {
            get
            {
                try
                {
                    var currentPackage = Windows.ApplicationModel.Package.Current;
                    return currentPackage != null && currentPackage.SignatureKind == PackageSignatureKind.Store;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
