using SpectriumLive.Application.Filters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;

public class GrayscaleFilterTests
{
    [Fact]
    public void Apply_OnColorPixel_ReturnsCorrectGrayscaleValue()
    {
        // Arrange
        var filter = new GrayscaleFilter();
        var colorPixel = new PixelData(100, 150, 200, 255);
        var imageBuffer = new ImageBuffer(1, 1, [colorPixel]);

        // Act
        filter.Apply(imageBuffer);

        // Assert
        var resultPixel = imageBuffer.Pixels[0];
        var expectedGrayValue = 140; // (0.299 * 100 + 0.587 * 150 + 0.114 * 200)

        Assert.Equal(expectedGrayValue, resultPixel.R);
        Assert.Equal(expectedGrayValue, resultPixel.G);
        Assert.Equal(expectedGrayValue, resultPixel.B);
        Assert.Equal(255, resultPixel.A);
    }
}
