using SpectriumLive.Application.Filters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;

public class NegativeFilterTests
{
    [Fact]
    public void Apply_OnWhitePixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new NegativeFilter();
        var whitePixel = new PixelData(255, 255, 255, 255);
        var image = new ImageBuffer(1, 1, [whitePixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(0, resultPixel.R); // 255 - 255 = 0
        Assert.Equal(0, resultPixel.G); // 255 - 255 = 0
        Assert.Equal(0, resultPixel.B); // 255 - 255 = 0
        Assert.Equal(255, resultPixel.A);
    }
}
