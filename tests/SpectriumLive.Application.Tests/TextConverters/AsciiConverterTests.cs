using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;
using Xunit;

namespace SpectriumLive.Application.Tests.TextConverters;

public class AsciiConverterTests
{
    public static TheoryData<char[], PixelData, char> GetPixelTestCases()
    {
        var data = new TheoryData<char[], PixelData, char>
        {
            // Black
            { AsciiCharacterRamps.Binary, new PixelData(0, 0, 0, 255), '#' },
            { AsciiCharacterRamps.Simple, new PixelData(0, 0, 0, 255), '#' },
            { AsciiCharacterRamps.Full, new PixelData(0, 0, 0, 255), '$' },

            // White
            { AsciiCharacterRamps.Binary, new PixelData(255, 255, 255, 255), ' ' },
            { AsciiCharacterRamps.Simple, new PixelData(255, 255, 255, 255), ' ' },
            { AsciiCharacterRamps.Full, new PixelData(255, 255, 255, 255), ' ' },

            // Mid-Gray
            { AsciiCharacterRamps.Binary, new PixelData(128, 128, 128, 255), '#' },
            { AsciiCharacterRamps.Simple, new PixelData(128, 128, 128, 255), '+' },
            { AsciiCharacterRamps.Full, new PixelData(128, 128, 128, 255), 'r' }
        };

        return data;
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
