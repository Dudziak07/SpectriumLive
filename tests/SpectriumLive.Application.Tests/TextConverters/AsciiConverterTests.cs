using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.TextConverters;

public class AsciiConverterTests
{
    public static IEnumerable<object[]> GetPixelTestCases()
    {
        // Black
        yield return new object[] {
            AsciiCharacterRamps.Binary,
            new PixelData(0, 0, 0, 255),
            '#'
        };
        yield return new object[] {
            AsciiCharacterRamps.Simple,
            new PixelData(0, 0, 0, 255),
            '#'
        };
        yield return new object[] {
            AsciiCharacterRamps.Full,
            new PixelData(0, 0, 0, 255),
            '$'
        };

        // White
        yield return new object[] {
            AsciiCharacterRamps.Binary,
            new PixelData(255, 255, 255, 255),
            ' '
        };
        yield return new object[] {
            AsciiCharacterRamps.Simple,
            new PixelData(255, 255, 255, 255),
            ' '
        };
        yield return new object[] {
            AsciiCharacterRamps.Full,
            new PixelData(255, 255, 255, 255),
            ' '
        };

        // Mid-Gray
        yield return new object[] {
            AsciiCharacterRamps.Binary,
            new PixelData(128, 128, 128, 255),
            '#'
        };
        yield return new object[] {
            AsciiCharacterRamps.Simple,
            new PixelData(128, 128, 128, 255),
            '+'
        };
        yield return new object[] {
            AsciiCharacterRamps.Full,
            new PixelData(128, 128, 128, 255),
            'r'
        };
    }

    [Theory]
    [MemberData(nameof(GetPixelTestCases))]
    public void Convert_WithPixel_ReturnsCorrectChar(char[] ramp, PixelData pixel, char expectedChar)
    {
        // Arrange
        var converter = new AsciiConverter("Test", ramp);
        var imageBuffer = new ImageBuffer(1, 1, [pixel]);
        var expected = expectedChar + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.Equal(expected, result);
    }
}
