using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class AsciiConverter(string name, char[] asciiChars) : IImageToTextConverter
{
    public string Name { get; } = name;

    public string Convert(ImageBuffer imageBuffer)
    {
        var rows = new string[imageBuffer.Height];
        Parallel.For(0, imageBuffer.Height, y =>
        {
            var rowBuilder = new StringBuilder();
            for ( int x = 0; x < imageBuffer.Width; x++ )
            {
                var pixel = imageBuffer.Pixels[y * imageBuffer.Width + x];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);

                int index = (int)Math.Round(grayValue / 255.0 * ( asciiChars.Length - 1 ));
                rowBuilder.Append(asciiChars[index]);
            }
            rows[y] = rowBuilder.ToString();
        });
        return string.Join(Environment.NewLine, rows);
    }
}
