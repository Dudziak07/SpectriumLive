using SpectriumLive.Application.Filters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class SwapChannelsFilterTests
{
    public static TheoryData<SwapMode, PixelData, PixelData> GetSwapTestData()
    {
        var inputPixel = new PixelData(200, 50, 100, 255);
        var data = new TheoryData<SwapMode, PixelData, PixelData>
        {
            { SwapMode.RB, inputPixel, new PixelData(100, 50, 200, 255) },
            { SwapMode.RG, inputPixel, new PixelData(50, 200, 100, 255) },
            { SwapMode.GB, inputPixel, new PixelData(200, 100, 50, 255) }
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(GetSwapTestData))]
    public void Apply_OnPixel_SwapsCorrectChannels(
        SwapMode mode,
        PixelData originalPixel,
        PixelData expectedPixel)
    {
        // Arrange
        var filter = new SwapChannelsFilter($"Test {mode}", mode);
        var image = new ImageBuffer(1, 1, [originalPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(expectedPixel.R, resultPixel.R);
        Assert.Equal(expectedPixel.G, resultPixel.G);
        Assert.Equal(expectedPixel.B, resultPixel.B);
        Assert.Equal(originalPixel.A, resultPixel.A);
    }
}
