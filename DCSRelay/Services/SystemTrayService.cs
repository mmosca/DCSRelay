using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows;

namespace DCSRelay.Services
{
    /// <summary>
    /// Service for managing system tray icon and window minimize-to-tray functionality.
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private DCSRelay.MainWindow? _mainWindow;

        /// <summary>
        /// Initializes the system tray icon with context menu.
        /// </summary>
        public void Initialize(DCSRelay.MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            // Create the context menu
            _contextMenu = new ContextMenuStrip();
            var restoreMenuItem = new ToolStripMenuItem("Restore", null, OnRestore);
            var exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);
            _contextMenu.Items.Add(restoreMenuItem);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(exitMenuItem);

            // Create the notify icon
            var trayIcon = LoadTrayIcon();

            _notifyIcon = new NotifyIcon
            {
                Icon = trayIcon,
                Visible = false,
                Text = "DCSRelay",
                ContextMenuStrip = _contextMenu
            };

            // Handle icon double-click to restore
            _notifyIcon.DoubleClick += (s, e) => OnRestore(null, EventArgs.Empty);

            // Handle window state changes
            _mainWindow.StateChanged += MainWindow_StateChanged;
        }

        private static Icon LoadTrayIcon()
        {
            var trayIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(trayIconPath))
            {
                return new Icon(trayIconPath);
            }

            var resourceUri = new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute);
            var resourceStream = System.Windows.Application.GetResourceStream(resourceUri);
            if (resourceStream?.Stream != null)
            {
                using (resourceStream.Stream)
                {
                    return (Icon)new Icon(resourceStream.Stream).Clone();
                }
            }

            return new Icon(SystemIcons.Application, 40, 40);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow?.WindowState == WindowState.Minimized)
            {
                // Hide window and show tray icon
                _mainWindow.Hide();
                _mainWindow.ShowInTaskbar = false;
                _notifyIcon!.Visible = true;
                System.Diagnostics.Debug.WriteLine("Application minimized to tray.");
            }
        }

        private void OnRestore(object? sender, EventArgs e)
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _notifyIcon!.Visible = false;
                System.Diagnostics.Debug.WriteLine("Application restored from tray.");
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Exit command invoked from tray context menu.");
            _mainWindow?.PrepareForApplicationShutdown();
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            if (_mainWindow != null)
            {
                _mainWindow.StateChanged -= MainWindow_StateChanged;
            }

            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}
