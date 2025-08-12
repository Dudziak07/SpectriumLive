using SpectriumLive.Application.Filters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Tests.Filters;

[TestFixture]
public class SepiaFilterTests
{
    [Test]
    public void Apply_OnMidGrayPixel_ReturnsCorrectSepiaValues()
    {
        // Arrange
        var filter = new SepiaFilter();
        var grayPixel = new PixelData(100, 100, 100, 255);
        var image = new ImageBuffer(1, 1, [grayPixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];

        Assert.Multiple(() =>
        {
            // newR = 0.393*R + 0.769*G + 0.189*B = 135
            // newG = 0.349*R + 0.686*G + 0.168*B = 120
            // newB = 0.272*R + 0.534*G + 0.131*B = 93
            Assert.That(resultPixel.R, Is.EqualTo(135));
            Assert.That(resultPixel.G, Is.EqualTo(120));
            Assert.That(resultPixel.B, Is.EqualTo(93));
            Assert.That(resultPixel.A, Is.EqualTo(255));
        });
    }
}
