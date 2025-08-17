using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Tests.TextConverters;

[TestFixture]
public class SimpleAsciiConverterTests
{
    private SimpleAsciiConverter converter = null!;

    [SetUp]
    public void Setup()
    {
        converter = new();
    }

    [Test]
    public void Convert_WithBlackPixel_ReturnsFirstAsciiChar()
    {
        // Arrange
        var blackPixel = new PixelData(0, 0, 0, 255);
        var imageBuffer = new ImageBuffer(1, 1, [blackPixel]);
        var expectedChar = "#" + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.That(result, Is.EqualTo(expectedChar));
    }

    [Test]
    public void Convert_WithWhitePixel_ReturnsLastAsciiChar()
    {
        // Arrange
        var whitePixel = new PixelData(255, 255, 255, 255);
        var imageBuffer = new ImageBuffer(1, 1, [whitePixel]);
        var expectedChar = " " + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.That(result, Is.EqualTo(expectedChar));
    }

    [Test]
    public void Convert_WithMidGrayPixel_ReturnsMiddleAsciiChar()
    {
        // Arrange
        var midGrayPixel = new PixelData(128, 128, 128, 255);
        var imageBuffer = new ImageBuffer(1, 1, [midGrayPixel]);
        var expectedChar = "+" + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.That(result, Is.EqualTo(expectedChar));
    }
}
