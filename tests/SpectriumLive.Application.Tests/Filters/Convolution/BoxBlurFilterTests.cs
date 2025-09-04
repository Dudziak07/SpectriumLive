using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters.Convolution;
public class BoxBlurFilterTests
{
    [Fact]
    public void Apply_On3x3Image_CenterPixelIsBlurred()
    {
        // Arrange
        var filter = new BoxBlurFilter();
        var pixels = new PixelData[9];

        for ( int i = 0; i < 8; i++ )
            pixels[i] = new PixelData(255, 255, 255, 255);
        pixels[8] = new PixelData(0, 0, 0, 255);
        var image = new ImageBuffer(3, 3, pixels);

        byte expectedValue = 226;

        // Act
        filter.Apply(image);

        // Assert
        var centerPixel = image.Pixels[4];

        Assert.Equal(expectedValue, centerPixel.R);
        Assert.Equal(expectedValue, centerPixel.G);
        Assert.Equal(expectedValue, centerPixel.B);
    }
}
