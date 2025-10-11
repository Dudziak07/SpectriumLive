using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;
using System.Text;

namespace SpectriumLive.Application.TextConverters;

public enum AsciiMode
{
    Binary,
    Simple,
    Full
}

public class AsciiConverter : IImageToTextConverter, INotifyPropertyChanged
{
    public string Name => "ASCII";
    public ImageProcessorGroup Group => ImageProcessorGroup.Ascii;
    public bool HasConfigurableProperties => true;

    private const double FontAspectRatio = 0.47;

    private AsciiMode asciiMode = AsciiMode.Simple; // Default mode
    public AsciiMode AsciiMode
    {
        get => asciiMode;
        set
        {
            if ( asciiMode != value )
            {
                asciiMode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AsciiMode)));
            }
        }
    }

    private int blockSize = 2; // Default block size
    public int BlockSize
    {
        get => blockSize;
        set
        {
            if ( blockSize != value )
            {
                blockSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BlockSize)));
            }
        }
    }

    public AsciiConverter() { }

    public string Convert(ImageBuffer imageBuffer)
    {
        char[] asciiChars = asciiMode switch
        {
            AsciiMode.Binary => AsciiCharacterRamps.Binary,
            AsciiMode.Simple => AsciiCharacterRamps.Simple,
            AsciiMode.Full => AsciiCharacterRamps.Full,
            _ => AsciiCharacterRamps.Simple
        };

        int validBlockSize = Math.Max(1, BlockSize);
        int widthInBlocks = imageBuffer.Width / validBlockSize;
        double aspectRatio = (double)imageBuffer.Height / imageBuffer.Width;
        int heightInBlocks = (int)Math.Ceiling(widthInBlocks * aspectRatio * FontAspectRatio);

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

    public string Convert(ImageBuffer imageBuffer, int blockSize)
    {
        BlockSize = blockSize;
        return Convert(imageBuffer);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
