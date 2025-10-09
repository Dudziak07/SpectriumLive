using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.TextConverters;

public class AsciiConverterTests
{
    public static TheoryData<AsciiMode, PixelData, char> GetPixelTestCases()
    {
        var data = new TheoryData<AsciiMode, PixelData, char>
        {
            // Black
            { AsciiMode.Binary, new PixelData(0, 0, 0, 255), '#' },
            { AsciiMode.Simple, new PixelData(0, 0, 0, 255), '#' },
            { AsciiMode.Full, new PixelData(0, 0, 0, 255), '$' },

            // White
            { AsciiMode.Binary, new PixelData(255, 255, 255, 255), ' ' },
            { AsciiMode.Simple, new PixelData(255, 255, 255, 255), ' ' },
            { AsciiMode.Full, new PixelData(255, 255, 255, 255), ' ' },

            // Mid-Gray
            { AsciiMode.Binary, new PixelData(128, 128, 128, 255), '#' },
            { AsciiMode.Simple, new PixelData(128, 128, 128, 255), '+' },
            { AsciiMode.Full, new PixelData(128, 128, 128, 255), 'r' }
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(GetPixelTestCases))]
    public void Convert_WithPixel_ReturnsCorrectChar(AsciiMode mode, PixelData pixel, char expectedChar)
    {
        // Arrange
        var converter = new AsciiConverter
        {
            AsciiMode = mode,
            BlockSize = 1
        };
        var imageBuffer = new ImageBuffer(1, 1, [pixel]);
        var expected = expectedChar + Environment.NewLine;

        // Act
        var result = converter.Convert(imageBuffer);

        // Assert
        Assert.Equal(expected, result);
    }
}
