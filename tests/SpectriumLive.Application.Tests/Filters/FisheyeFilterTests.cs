using SpectriumLive.Application.Filters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Filters;
public class FisheyeFilterTests
{
    public static TheoryData<double> StrengthCases =>
    [
        1.0, // Fisheye
        -1.0 //Defisheye
    ];

    [Theory]
    [MemberData(nameof(StrengthCases))]
    public void Apply_CenterPixel_RemainsUnchanged(double strength)
    {
        // Arrange
        var centerPixel = new PixelData(100, 150, 200, 255);
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), 25).ToArray();
        pixels[12] = centerPixel; // center 5x5
        var image = new ImageBuffer(5, 5, pixels);
        var filter = new FisheyeFilter(strength);

        // Act
        filter.Apply(image);

        // Assert
        var result = image.Pixels[12];
        Assert.Equal(centerPixel.R, result.R);
        Assert.Equal(centerPixel.G, result.G);
        Assert.Equal(centerPixel.B, result.B);
        Assert.Equal(centerPixel.A, result.A);
    }

    [Theory]
    [MemberData(nameof(StrengthCases))]
    public void Apply_CornerPixels_AreBlack(double strength)
    {
        // Arrange
        var pixels = Enumerable.Repeat(new PixelData(100, 150, 200, 255), 25).ToArray();
        var image = new ImageBuffer(5, 5, pixels);
        var filter = new FisheyeFilter(strength);

        // Act
        filter.Apply(image);

        // Assert
        int[] corners = { 0, 4, 20, 24 };
        foreach ( var i in corners )
        {
            Assert.Equal(0, image.Pixels[i].R);
            Assert.Equal(0, image.Pixels[i].G);
            Assert.Equal(0, image.Pixels[i].B);
        }
    }

    [Theory]
    [MemberData(nameof(StrengthCases))]
    public void Apply_DistanceFromCenter_IsTransformed(double strength)
    {
        // Arrange
        int size = 101;
        var pixels = Enumerable.Repeat(new PixelData(0, 0, 0, 255), size * size).ToArray();

        int centerX = size / 2;
        int centerY = size / 2;
        int testY = centerY - 20;
        pixels[testY * size + centerX] = new PixelData(255, 255, 255, 255);

        var image = new ImageBuffer(size, size, pixels);
        var filter = new FisheyeFilter(strength);

        // Act
        filter.Apply(image);

        // Assert
        Assert.NotEqual(255, image.Pixels[testY * size + centerX].R);

        bool found = image.Pixels.Any(p => p.R == 255 && p.G == 255 && p.B == 255);
        Assert.True(found, "Pixel should have been remapped somewhere.");
    }

    [Fact]
    public void CornerR_ShouldBeGreaterThanOne()
    {
        // Arrange
        int w = 5, h = 5;
        double cx = ( w - 1 ) / 2.0;
        double cy = ( h - 1 ) / 2.0;
        double radius = Math.Min(cx, cy);

        // Act
        double dx = ( 0 - cx ) / radius;
        double dy = ( 0 - cy ) / radius;
        double r = Math.Sqrt(dx * dx + dy * dy);

        // Assert
        Assert.True(r > 1.0, $"r for corner = {r} (expected > 1)");
    }
}
