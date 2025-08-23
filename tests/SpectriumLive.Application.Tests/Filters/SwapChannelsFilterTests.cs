using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class SwapChannelsFilterTests
{
    [Fact]
    public void Apply_OnPixel_SwapRedAndBlueChannels()
    {
        // Arrange
        var filter = new SwapChannelsFilter();
        var originalPixel = new PixelData(200, 50, 100, 255);
        var image = new ImageBuffer(1, 1, [originalPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(originalPixel.B, resultPixel.R);
        Assert.Equal(originalPixel.G, resultPixel.G);
        Assert.Equal(originalPixel.R, resultPixel.B);
        Assert.Equal(originalPixel.A, resultPixel.A);
    }
}
