using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class FisheyeFilterTests
{
    [Fact]
    public void Apply_CenterPixel_RemainsUnchanged()
    {
        // Arrange
        var centerPixel = new PixelData(100, 150, 200, 255);
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 9).ToArray();
        pixels[4] = centerPixel; // center 3x3
        var image = new ImageBuffer(3, 3, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        Assert.Equal(centerPixel, image.Pixels[4]);
    }

    [Fact]
    public void Apply_CornerPixels_AreBlack()
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(100, 150, 200, 255), 9).ToArray();
        var image = new ImageBuffer(3, 3, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        Assert.Equal(new PixelData(0, 0, 0, 255), image.Pixels[0]); // left top
        Assert.Equal(new PixelData(0, 0, 0, 255), image.Pixels[2]); // right top
        Assert.Equal(new PixelData(0, 0, 0, 255), image.Pixels[6]); // left down
        Assert.Equal(new PixelData(0, 0, 0, 255), image.Pixels[8]); // right down
    }

    [Fact]
    public void Apply_EdgePixel_MovesTowardCenter()
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 9).ToArray();
        var edgePixel = new PixelData(50, 100, 150, 255);
        pixels[1] = edgePixel; // center top
        var image = new ImageBuffer(3, 3, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        Assert.Contains(image.Pixels, p => p.Equals(edgePixel));
    }
}
