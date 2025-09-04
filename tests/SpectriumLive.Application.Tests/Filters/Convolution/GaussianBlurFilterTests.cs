using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters.Convolution;
public class GaussianBlurFilterTests
{
    [Fact]
    public void Apply_OnImage_AppliesBlurEffect()
    {
        // Arrange
        var filter = new GaussianBlurFilter();
        var pixels = new PixelData[25];

        for ( int i = 0; i < pixels.Length; i++ )
            pixels[i] = new PixelData(0, 0, 0, 255);
        pixels[12] = new PixelData(255, 255, 255, 255);
        var image = new ImageBuffer(5, 5, pixels);

        byte expectedValue = 38;

        // Act
        filter.Apply(image);

        // Assert
        var centerPixel = image.Pixels[12];

        Assert.Equal(expectedValue, centerPixel.R);
        Assert.Equal(expectedValue, centerPixel.G);
        Assert.Equal(expectedValue, centerPixel.B);
    }
}
