using SpectriumLive.Application.Filters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Tests.Filters;

[TestFixture]
public class GrayscaleFilterTests
{
    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That(resultPixel.R, Is.EqualTo(expectedGrayValue));
            Assert.That(resultPixel.G, Is.EqualTo(expectedGrayValue));
            Assert.That(resultPixel.B, Is.EqualTo(expectedGrayValue));
            Assert.That(resultPixel.A, Is.EqualTo(255));
        });
    }
}
