using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DCSRelay.Services;

public static class SvgIconLoader
{
    public static ImageSource? LoadFromAssets(string fileName)
    {
        var resourceInfo = System.Windows.Application.GetResourceStream(new Uri($"Assets/{fileName}", UriKind.Relative));
        var resourceStream = resourceInfo?.Stream;
        if (resourceStream == null)
        {
            return null;
        }

        var settings = new WpfDrawingSettings
        {
            IncludeRuntime = false,
            TextAsGeometry = true
        };

        var tempPath = Path.Combine(Path.GetTempPath(), $"dcsrelay-{Guid.NewGuid():N}.svg");

        try
        {
            using (resourceStream)
            using (var output = File.Create(tempPath))
            {
                resourceStream.CopyTo(output);
            }

            var reader = new FileSvgReader(settings);
            var drawing = reader.Read(tempPath);
            if (drawing == null)
            {
                return null;
            }

            drawing.Freeze();
            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
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
