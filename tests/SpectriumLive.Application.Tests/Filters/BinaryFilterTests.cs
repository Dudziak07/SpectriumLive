using SpectriumLive.Application.Filters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Tests.Filters;

public class BinaryFilterTests
{
    private const byte Threshold = 128;

    [Test]
    public void Apply_OnDarkPixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var darkPixel = new PixelData(100, 130, 140, 255);
        var image = new ImageBuffer(1, 1, [darkPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Multiple(() =>
        {
            Assert.That(resultPixel.R, Is.EqualTo(0));
            Assert.That(resultPixel.G, Is.EqualTo(0));
            Assert.That(resultPixel.B, Is.EqualTo(0));
            Assert.That(resultPixel.A, Is.EqualTo(255));
        });
    }

    [Test]
    public void Apply_OnLightPixel_ReturnsWhitePixel()
    {
        // Arrange
        var filter = new BinaryFilter();
        var lightPixel = new PixelData(100, 150, 200, 255);
        var image = new ImageBuffer(1, 1, [lightPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Multiple(() =>
        {
            Assert.That(resultPixel.R, Is.EqualTo(255));
            Assert.That(resultPixel.G, Is.EqualTo(255));
            Assert.That(resultPixel.B, Is.EqualTo(255));
            Assert.That(resultPixel.A, Is.EqualTo(255));
        });
    }
}
