namespace SpectriumLive.Application.Tests;

[TestFixture]
public class NegativeFilterTests
{
    [Test]
    public void Apply_OnWhitePixel_ReturnsBlackPixel()
    {
        // Arrange
        var filter = new NegativeFilter();
        var whitePixel = new Pixel(255, 255, 255);
        var image = new ImageBuffer(1, 1, [whitePixel]);

        // Act
        filter.Apply(image);

        // Assert
        var resultPixel = image.Pixels[0];
        Assert.That(resultPixel.R, Is.EqualTo(0));
        Assert.That(resultPixel.G, Is.EqualTo(0));
        Assert.That(resultPixel.B, Is.EqualTo(0));
        Assert.That(resultPixel.A, Is.EqualTo(255));
    }
}
