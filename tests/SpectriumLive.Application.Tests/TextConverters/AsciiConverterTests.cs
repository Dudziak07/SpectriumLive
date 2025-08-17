using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Tests.TextConverters;

[TestFixture]
public class AsciiConverterTests
{
    private static IEnumerable<TestCaseData> GetPixelTestCases()
    {
        yield return new TestCaseData(
            AsciiCharacterRamps.Binary,
            new PixelData(0, 0, 0, 255),
            '#'
        ).SetName("Black Pixel (Binary Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Simple,
            new PixelData(0, 0, 0, 255),
            '#'
        ).SetName("Black Pixel (Simple Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Full,
            new PixelData(0, 0, 0, 255),
            '$'
        ).SetName("Black Pixel (Full Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Binary,
            new PixelData(255, 255, 255, 255),
            ' '
        ).SetName("White Pixel (Binary Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Simple,
            new PixelData(255, 255, 255, 255),
            ' '
        ).SetName("White Pixel (Simple Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Full,
            new PixelData(255, 255, 255, 255),
            ' '
        ).SetName("White Pixel (Full Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Binary,
            new PixelData(128, 128, 128, 255),
            '#'
        ).SetName("Mid-Gray Pixel (Binary Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Simple,
            new PixelData(128, 128, 128, 255),
            '+'
        ).SetName("Mid-Gray Pixel (Simple Ramp)");

        yield return new TestCaseData(
            AsciiCharacterRamps.Full,
            new PixelData(128, 128, 128, 255),
            'r'
        ).SetName("Mid-Gray Pixel (Full Ramp)");
    }

    [TestCaseSource(nameof(GetPixelTestCases))]
    public void Convert_WithPixel_ReturnsCorrectChar(char[] ramp, PixelData pixel, char expectedChar)
    {
        // Arrange
        var converter = new AsciiConverter("Test", ramp);
        var imageBuffer = new ImageBuffer(1, 1, [pixel]);
        var expected = expectedChar + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
