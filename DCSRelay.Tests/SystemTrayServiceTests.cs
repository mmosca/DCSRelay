using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using DCSRelay.Services;

namespace DCSRelay.Tests;

public class SystemTrayServiceTests
{
    [Fact]
    public void LoadTrayIcon_uses_project_icon_when_icon_exists_in_assets_output()
    {
        var projectIconPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DCSRelay", "Assets", "AppIcon.ico"));

        Assert.True(File.Exists(projectIconPath), $"Project icon was not found at '{projectIconPath}'.");

        var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
        Directory.CreateDirectory(assetsDirectory);

        var outputIconPath = Path.Combine(assetsDirectory, "AppIcon.ico");
        var originalOutputIconBytes = File.Exists(outputIconPath)
            ? File.ReadAllBytes(outputIconPath)
            : null;

        File.Copy(projectIconPath, outputIconPath, overwrite: true);

        try
        {
            using var expectedIcon = new Icon(projectIconPath);
            using var actualIcon = InvokeLoadTrayIcon();

            var expectedHash = GetIconHash(expectedIcon);
            var actualHash = GetIconHash(actualIcon);

            Assert.Equal(expectedHash, actualHash);
        }
        finally
        {
            if (originalOutputIconBytes is null)
            {
                if (File.Exists(outputIconPath))
                {
                    File.Delete(outputIconPath);
                }
            }
            else
            {
                File.WriteAllBytes(outputIconPath, originalOutputIconBytes);
            }
        }
    }

    private static Icon InvokeLoadTrayIcon()
    {
        var method = typeof(SystemTrayService).GetMethod("LoadTrayIcon", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var icon = method.Invoke(null, null);

        return Assert.IsType<Icon>(icon);
    }

    private static string GetIconHash(Icon icon)
    {
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();

        bitmap.Save(stream, ImageFormat.Png);

        var hash = SHA256.HashData(stream.ToArray());
        return Convert.ToHexString(hash);
    }
}
