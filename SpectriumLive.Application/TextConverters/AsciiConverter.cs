using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

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
        string asciiChars = asciiMode switch
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

        int maxCharIndex = asciiChars.Length - 1;
        char[] charLut = new char[256];
        for (int g = 0; g < 256; g++)
        {
            int index = (int)Math.Round(g / 255.0 * maxCharIndex);
            charLut[g] = asciiChars[index];
        }

        string nl = Environment.NewLine;
        int nlLen = nl.Length;
        int lineLength = widthInBlocks + nlLen;

        var rows = new string[heightInBlocks];

        if (heightInBlocks < 64)
        {
            for (int y = 0; y < heightInBlocks; y++)
            {
                rows[y] = CreateRowString(y, widthInBlocks, validBlockSize, nl, nlLen, lineLength, imageBuffer, charLut);
            }
        }
        else
        {
            Parallel.For(0, heightInBlocks, y =>
            {
                rows[y] = CreateRowString(y, widthInBlocks, validBlockSize, nl, nlLen, lineLength, imageBuffer, charLut);
            });
        }

        return string.Concat(rows);
    }

    private static string CreateRowString(
        int y,
        int widthInBlocks,
        int validBlockSize,
        string nl,
        int nlLen,
        int lineLength,
        ImageBuffer imageBuffer,
        char[] charLut)
    {
        int sourceY = (int)( y / FontAspectRatio * validBlockSize );
        sourceY = Math.Min(sourceY, imageBuffer.Height - 1);
        int rowOffset = sourceY * imageBuffer.Width;

        return string.Create(lineLength, (rowOffset, imageBuffer, validBlockSize, charLut, widthInBlocks, nl, nlLen), static (span, state) =>
        {
            var (rOffset, imgBuffer, vBlockSize, lut, wBlocks, newlineStr, newlineLen) = state;
            var pixels = imgBuffer.Pixels;
            int imgWidth = imgBuffer.Width;

            for (int x = 0; x < wBlocks; x++)
            {
                int sourceX = Math.Min(x * vBlockSize, imgWidth - 1);
                var pixel = pixels[rOffset + sourceX];
                byte grayValue = ColorCalculator.ToGrayscale(pixel);
                span[x] = lut[grayValue];
            }

            for (int k = 0; k < newlineLen; k++)
                span[wBlocks + k] = newlineStr[k];
        });
    }

    public string Convert(ImageBuffer imageBuffer, int blockSize)
    {
        BlockSize = blockSize;
        return Convert(imageBuffer);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
