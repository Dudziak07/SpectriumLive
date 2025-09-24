using SpectriumLive.Application.Filters;
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
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 25).ToArray();
        pixels[12] = centerPixel; // center 5x5
        var image = new ImageBuffer(5, 5, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        var result = image.Pixels[12];
        Assert.Equal(centerPixel.R, result.R);
        Assert.Equal(centerPixel.G, result.G);
        Assert.Equal(centerPixel.B, result.B);
        Assert.Equal(centerPixel.A, result.A);
    }

    [Fact]
    public void Apply_CornerPixels_AreBlack()
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(100, 150, 200, 255), 25).ToArray();
        var image = new ImageBuffer(5, 5, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        Assert.Equal(0, image.Pixels[0].R);
        Assert.Equal(0, image.Pixels[0].G);
        Assert.Equal(0, image.Pixels[0].B);

        Assert.Equal(0, image.Pixels[4].R);
        Assert.Equal(0, image.Pixels[4].G);
        Assert.Equal(0, image.Pixels[4].B);

        Assert.Equal(0, image.Pixels[20].R);
        Assert.Equal(0, image.Pixels[20].G);
        Assert.Equal(0, image.Pixels[20].B);

        Assert.Equal(0, image.Pixels[24].R);
        Assert.Equal(0, image.Pixels[24].G);
        Assert.Equal(0, image.Pixels[24].B);
    }

    [Fact]
    public void Apply_EdgePixel_MovesTowardCenter()
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 25).ToArray();
        var edgePixel = new PixelData(50, 100, 150, 255);
        pixels[7] = edgePixel; // (2, 1)
        var image = new ImageBuffer(5, 5, pixels);
        var filter = new FisheyeFilter();

        // Act
        filter.Apply(image);

        // Assert
        var centralIndices = new[] { 6, 7, 8, 11, 12, 13, 16, 17, 18 }; // central 3x3
        bool found = centralIndices.Any(i =>
            image.Pixels[i].R == edgePixel.R &&
            image.Pixels[i].G == edgePixel.G &&
            image.Pixels[i].B == edgePixel.B &&
            image.Pixels[i].A == edgePixel.A);

        Assert.True(found, "Edge pixel was not mapped into central region after fisheye transform.");
    }

    [Fact]
    public void CornerR_ShouldBeGreaterThanOne()
    {
        // Arrange
        int w = 5, h = 5;
        double cx = ( w - 1 ) / 2.0;
        double cy = ( h - 1 ) / 2.0;
        double radius = Math.Min(cx, cy);

        // Act
        double dx = ( 0 - cx ) / radius;
        double dy = ( 0 - cy ) / radius;
        double r = Math.Sqrt(dx * dx + dy * dy);

        // Assert
        Assert.True(r > 1.0, $"r for corner = {r} (expected > 1)");
    }

    [Fact]
    public void PixelsGetter_ReturnsReference_NotCopy()
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(10, 20, 30, 255), 25).ToArray();
        var image = new ImageBuffer(5, 5, pixels);

        // Act
        var arr = image.Pixels;
        arr[0] = new PixelData(1, 2, 3, 4);

        // Assert
        var arr2 = image.Pixels;
        Assert.Equal(1, arr2[0].R);
    }
}
