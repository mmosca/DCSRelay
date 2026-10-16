using System.IO;
using System.Windows.Media;
using DCSRelay.Services;

namespace DCSRelay.Tests;

public class SvgIconLoaderTests
{
    [Fact]
    public void LoadFromAssets_returns_null_when_file_does_not_exist()
    {
        var result = SvgIconLoader.LoadFromAssets($"missing-{Guid.NewGuid():N}.svg");

        Assert.Null(result);
    }

    [Fact]
    public void LoadFromAssets_returns_frozen_drawing_image_for_valid_svg()
    {
        var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets");
        Directory.CreateDirectory(assetsDirectory);

        var fileName = $"test-icon-{Guid.NewGuid():N}.svg";
        var filePath = Path.Combine(assetsDirectory, fileName);

        var svg = """
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" fill="none">
                    <circle cx="32" cy="32" r="20" stroke="#58FFA0" stroke-width="8"/>
                    <line x1="20" y1="32" x2="44" y2="32" stroke="#8BFFC0" stroke-width="8" stroke-linecap="butt"/>
                    <line x1="32" y1="20" x2="32" y2="44" stroke="#8BFFC0" stroke-width="8" stroke-linecap="butt"/>
                  </svg>
                  """;

        File.WriteAllText(filePath, svg);

        try
        {
            var result = SvgIconLoader.LoadFromAssets(fileName);

            var drawingImage = Assert.IsType<DrawingImage>(result);
            Assert.True(drawingImage.IsFrozen);
            Assert.NotNull(drawingImage.Drawing);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
