using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class SimpleAsciiConverter
{
    public static string Name => "ASCII (Simple)";
    private static readonly char[] AsciiChars = ['#', '@', '%', '=', '+', '*', ':', '-', '.', ' '];

    public static string Convert(ImageBuffer imageBuffer)
    {
        var result = new StringBuilder();

        for ( int y = 0; y < imageBuffer.Height; y++ )
        {
            for ( int x = 0; x < imageBuffer.Width; x++ )
            {
                var pixel = imageBuffer.Pixels[y * imageBuffer.Width + x];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);

                int index = grayValue * ( AsciiChars.Length - 1 ) / 255;
                result.Append(AsciiChars[index]);
            }
            result.AppendLine();
        }
        return result.ToString();
    }
}
