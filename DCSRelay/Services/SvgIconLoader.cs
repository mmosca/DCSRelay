using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DCSRelay.Services;

public static class SvgIconLoader
{
    public static ImageSource? LoadFromAssets(string fileName)
    {
        var settings = new WpfDrawingSettings
        {
            IncludeRuntime = false,
            TextAsGeometry = true
        };

        var reader = new FileSvgReader(settings);

        // Preferred: load from WPF resources
        try
        {
            var resourceInfo = System.Windows.Application.GetResourceStream(new Uri($"Assets/{fileName}", UriKind.Relative));
            var resourceStream = resourceInfo?.Stream;
            if (resourceStream != null)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"dcsrelay-{Guid.NewGuid():N}.svg");
                try
                {
                    using (resourceStream)
                    using (var output = File.Create(tempPath))
                    {
                        resourceStream.CopyTo(output);
                    }

                    return ReadAndFreeze(reader, tempPath);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
        }
        catch (IOException)
        {
            // Missing resource -> fallback to file-based lookup (used by tests).
        }

        // Fallback for tests/local file-based scenarios
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(iconPath))
        {
            return null;
        }

        return ReadAndFreeze(reader, iconPath);
    }

    private static ImageSource? ReadAndFreeze(FileSvgReader reader, string path)
    {
        var drawing = reader.Read(path);
        if (drawing == null)
        {
            return null;
        }

        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
