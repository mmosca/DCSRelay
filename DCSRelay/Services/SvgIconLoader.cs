using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DCSRelay.Services;

public static class SvgIconLoader
{
    public static ImageSource? LoadFromAssets(string fileName)
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(iconPath))
        {
            return null;
        }

        var settings = new WpfDrawingSettings
        {
            IncludeRuntime = false,
            TextAsGeometry = true
        };

        var reader = new FileSvgReader(settings);
        var drawing = reader.Read(iconPath);
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
