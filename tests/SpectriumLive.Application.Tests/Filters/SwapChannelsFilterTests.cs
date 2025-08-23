using SpectriumLive.Application.Filters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class SwapChannelsFilterTests
{
    [Theory]
    [InlineData(SwapMode.RB, 200, 50, 100, 100, 50, 200)]
    [InlineData(SwapMode.RG, 200, 50, 100, 50, 200, 100)]
    [InlineData(SwapMode.GB, 200, 50, 100, 200, 100, 50)]
    public void Apply_OnPixel_SwapsCorrectChannels(
        SwapMode mode,
        byte rIn, byte gIn, byte bIn,
        byte rOut, byte gOut, byte bOut)
    {
        // Arrange
        var filter = new SwapChannelsFilter($"Test {mode}", mode);
        var originalPixel = new PixelData(rIn, gIn, bIn, 255);
        var image = new ImageBuffer(1, 1, [originalPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(rOut, resultPixel.R);
        Assert.Equal(gOut, resultPixel.G);
        Assert.Equal(bOut, resultPixel.B);
        Assert.Equal(originalPixel.A, resultPixel.A);
    }
}
