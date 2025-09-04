using SpectriumLive.Application.Filters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;

public class BinaryFilterTests
{
    private const byte Threshold = 128;

    [Fact]
    public void Apply_OnDarkPixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var darkPixel = new PixelData(100, 130, 140, 255);
        var image = new ImageBuffer(1, 1, [darkPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(0, resultPixel.R);
        Assert.Equal(0, resultPixel.G);
        Assert.Equal(0, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }

    [Fact]
    public void Apply_OnLightPixel_ReturnsWhitePixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var lightPixel = new PixelData(100, 150, 200, 255);
        var image = new ImageBuffer(1, 1, [lightPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(255, resultPixel.R);
        Assert.Equal(255, resultPixel.G);
        Assert.Equal(255, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }
}
