using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class FullAsciiConverter : IImageToTextConverter
{
    public string Name => "ASCII (Full)";
    private static readonly char[] AsciiChars = [
        '$', '@', 'B', 'P', 'W', 'M', 'Q', 'H', 'D', 'k',
        'h', 'q', 'd', 'p', 'b', 'w', 'm', 'Z', '0', 'O',
        'L', 'C', 'J', 'U', 'Y', 'X', 'z', 'c', 'v', 'u',
        'n', 'x', 'r', 'j', 'f', 't', '/', '\\', '|', '(',
        ')', '1', '{', '}', '[', ']', '?', '-', '_', '+',
        '~', '<', '>', 'i', '!', 'l', 'I', ';', ':', ',',
        '"', '^', '`', '\'', '.', ' '
    ];

    public string Convert(ImageBuffer imageBuffer)
    {
        var result = new StringBuilder();

        for ( int y = 0; y < imageBuffer.Height; y++ )
        {
            for ( int x = 0; x < imageBuffer.Width; x++ )
            {
                var pixel = imageBuffer.Pixels[y * imageBuffer.Width + x];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);

                int index = (int)Math.Round(grayValue / 255.0 * ( AsciiChars.Length - 1 ));
                result.Append(AsciiChars[index]);
            }
            result.AppendLine();
        }
        return result.ToString();
    }
}
