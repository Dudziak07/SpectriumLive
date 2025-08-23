using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters.Convolution;
public class EdgeDetectionFilterTests
{
    [Fact]
    public void Apply_OnSquare_HighlightsEdges()
    {
        // Arrange
        var filter = new EdgeDetectionFilter();
        var pixels = new PixelData[25];

        for ( int i = 0; i < 25; i++ )
            pixels[i] = new PixelData(255, 255, 255, 255);
        pixels[6] = pixels[7] = pixels[8] = new PixelData(0, 0, 0, 255);
        pixels[11] = pixels[12] = pixels[13] = new PixelData(0, 0, 0, 255);
        pixels[16] = pixels[17] = pixels[18] = new PixelData(0, 0, 0, 255);
        var image = new ImageBuffer(5, 5, pixels);

        // Act
        filter.Apply(image);

        // Assert
        Assert.Equal(0, image.Pixels[12].R);
        Assert.True(image.Pixels[6].R > 0);
    }
}
