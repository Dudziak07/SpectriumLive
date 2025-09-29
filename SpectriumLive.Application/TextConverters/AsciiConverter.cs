using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Text;

namespace SpectriumLive.Application.TextConverters;
public class AsciiConverter(string name, char[] asciiChars) : IImageToTextConverter
{
    public string Name { get; } = name;

    public string Convert(ImageBuffer imageBuffer, int blockSize)
    {
        int validBlockSize = Math.Max(1, blockSize);
        int widthInBlocks = imageBuffer.Width / validBlockSize;
        int heightInBlocks = imageBuffer.Height / validBlockSize;

        var rows = new string[heightInBlocks];
        Parallel.For(0, heightInBlocks, y =>
        {
            var rowBuilder = new StringBuilder(widthInBlocks);
            for ( int x = 0; x < widthInBlocks; x++ )
            {
                double totalGray = 0;
                int pixelCount = 0;

                int startY = y * validBlockSize;
                int startX = x * validBlockSize;

                for ( int blockY = 0; blockY < validBlockSize; blockY++ )
                {
                    for ( int blockX = 0; blockX < validBlockSize; blockX++ )
                    {
                        int currentX = startX + blockX;
                        int currentY = startY + blockY;

                        if ( currentX < imageBuffer.Width && currentY < imageBuffer.Height )
                        {
                            var pixel = imageBuffer.Pixels[currentY * imageBuffer.Width + currentX];
                            totalGray += ColorCalculator.ToGrayscale(pixel);
                            pixelCount++;
                        }
                    }
                }

                if ( pixelCount > 0 )
                {
                    byte avgGray = (byte)( totalGray / pixelCount );
                    int index = (int)Math.Round(avgGray / 255.0 * ( asciiChars.Length - 1 ));
                    rowBuilder.Append(asciiChars[index]);
                }
            }
            rows[y] = rowBuilder.ToString();
        });

        return string.Join(Environment.NewLine, rows) + Environment.NewLine;
    }

    public string Convert(ImageBuffer imageBuffer) => Convert(imageBuffer, 1);
}
