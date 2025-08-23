using SpectriumLive.Application.Filters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class ChannelFilterTests
{
    public static IEnumerable<object[]> GetChannelTestData()
    {
        var inputPixel = new PixelData(200, 50, 100, 255);
        yield return new object[] { ChannelSelector.R, inputPixel, new PixelData(200, 0, 0, 255) };
        yield return new object[] { ChannelSelector.G, inputPixel, new PixelData(0, 50, 0, 255) };
        yield return new object[] { ChannelSelector.B, inputPixel, new PixelData(0, 0, 100, 255) };
    }

    [Theory]
    [MemberData(nameof(GetChannelTestData))]
    public void Apply_WithGivenChannel_KeepsOnlyThatChannel(
        ChannelSelector mode,
        PixelData originalPixel,
        PixelData expectedPixel)
    {
        // Arrange
        var filter = new ChannelFilterTests($"Test {mode}", mode);
        var image = new ImageBuffer(1, 1, [originalPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Equal(expectedPixel.R, resultPixel.R);
        Assert.Equal(expectedPixel.G, resultPixel.G);
        Assert.Equal(expectedPixel.B, resultPixel.B);
        Assert.Equal(expectedPixel.A, resultPixel.A);
    }
}
