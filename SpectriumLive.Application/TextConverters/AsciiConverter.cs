using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class AsciiConverter(string name, char[] ramp) : IImageToTextConverter
{
    public string Name { get; } = name;

    public string Convert(ImageBuffer imageBuffer)
    {
        var result = new StringBuilder();
        for ( int y = 0; y < imageBuffer.Height; y++ )
        {
            for ( int x = 0; x < imageBuffer.Width; x++ )
            {
                var pixel = imageBuffer.Pixels[y * imageBuffer.Width + x];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);

                int index = (int)Math.Round(grayValue / 255.0 * ( ramp.Length - 1 ));
                result.Append(ramp[index]);
            }
            result.AppendLine();
        }
        return result.ToString();
    }
}
