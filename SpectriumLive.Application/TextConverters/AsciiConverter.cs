using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class AsciiConverter(string name, char[] asciiChars) : IImageToTextConverter
{
    public string Name { get; } = name;
    public ImageProcessorGroup Group => ImageProcessorGroup.Ascii;

    private const double FontAspectRatio = 0.47;

    public string Convert(ImageBuffer imageBuffer, int blockSize)
    {
        int validBlockSize = Math.Max(1, blockSize);
        int widthInBlocks = imageBuffer.Width / validBlockSize;
        double aspectRatio = (double)imageBuffer.Height / imageBuffer.Width;
        int heightInBlocks = (int)( widthInBlocks * aspectRatio * FontAspectRatio );

        if ( widthInBlocks == 0 || heightInBlocks == 0 )
            return string.Empty;

        var rows = new string[heightInBlocks];

        Parallel.For(0, heightInBlocks, y =>
        {
            var rowBuilder = new StringBuilder(widthInBlocks);
            for ( int x = 0; x < widthInBlocks; x++ )
            {
                int sourceX = x * validBlockSize;
                int sourceY = (int)( y / FontAspectRatio * validBlockSize );

                sourceX = Math.Min(sourceX, imageBuffer.Width - 1);
                sourceY = Math.Min(sourceY, imageBuffer.Height - 1);

                var pixel = imageBuffer.Pixels[sourceY * imageBuffer.Width + sourceX];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);

                int index = (int)Math.Round(grayValue / 255.0 * ( asciiChars.Length - 1 ));
                rowBuilder.Append(asciiChars[index]);
            }
            rows[y] = rowBuilder.ToString();
        });

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    public string Convert(ImageBuffer imageBuffer) => Convert(imageBuffer, 1);
}
