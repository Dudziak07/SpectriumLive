using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Buffers;
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

    private static readonly char[] BinaryLut = BuildCharLut(AsciiCharacterRamps.Binary);
    private static readonly char[] SimpleLut = BuildCharLut(AsciiCharacterRamps.Simple);
    private static readonly char[] FullLut = BuildCharLut(AsciiCharacterRamps.Full);

    private static char[] BuildCharLut(string asciiChars)
    {
        char[] lut = new char[256];
        int maxCharIndex = asciiChars.Length - 1;
        for (int g = 0; g < 256; g++)
        {
            int index = (int)Math.Round(g / 255.0 * maxCharIndex);
            lut[g] = asciiChars[index];
        }
        return lut;
    }

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
        char[] charLut = asciiMode switch
        {
            AsciiMode.Binary => BinaryLut,
            AsciiMode.Simple => SimpleLut,
            AsciiMode.Full => FullLut,
            _ => SimpleLut
        };

        int validBlockSize = Math.Max(1, BlockSize);
        int widthInBlocks = imageBuffer.Width / validBlockSize;
        double aspectRatio = (double)imageBuffer.Height / imageBuffer.Width;
        int heightInBlocks = (int)Math.Ceiling(widthInBlocks * aspectRatio * FontAspectRatio);

        if ( widthInBlocks == 0 || heightInBlocks == 0 )
            return string.Empty;

        string nl = Environment.NewLine;
        int nlLen = nl.Length;
        int lineLength = widthInBlocks + nlLen;
        int totalChars = heightInBlocks * lineLength;

        char[] charBuffer = ArrayPool<char>.Shared.Rent(totalChars);

        try
        {
            var pixels = imageBuffer.Pixels;
            int imgWidth = imageBuffer.Width;
            int imgHeight = imageBuffer.Height;

            if (heightInBlocks < 64 || Environment.ProcessorCount <= 1)
            {
                for (int y = 0; y < heightInBlocks; y++)
                {
                    FillRow(y, charBuffer, y * lineLength, widthInBlocks, validBlockSize, nl, nlLen, pixels, imgWidth, imgHeight, charLut);
                }
            }
            else
            {
                Parallel.For(0, heightInBlocks, y =>
                {
                    FillRow(y, charBuffer, y * lineLength, widthInBlocks, validBlockSize, nl, nlLen, pixels, imgWidth, imgHeight, charLut);
                });
            }

            return new string(charBuffer, 0, totalChars);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    private static void FillRow(
        int y,
        char[] charBuffer,
        int destOffset,
        int widthInBlocks,
        int validBlockSize,
        string nl,
        int nlLen,
        PixelData[] pixels,
        int imgWidth,
        int imgHeight,
        char[] charLut)
    {
        int sourceY = (int)( y / FontAspectRatio * validBlockSize );
        sourceY = Math.Min(sourceY, imgHeight - 1);
        int rowOffset = sourceY * imgWidth;

        int sourceX = 0;
        int maxSourceX = imgWidth - 1;

        for (int x = 0; x < widthInBlocks; x++)
        {
            int sx = Math.Min(sourceX, maxSourceX);
            var pixel = pixels[rowOffset + sx];
            byte grayValue = ColorCalculator.ToGrayscale(pixel);
            charBuffer[destOffset + x] = charLut[grayValue];
            sourceX += validBlockSize;
        }

        for (int k = 0; k < nlLen; k++)
            charBuffer[destOffset + widthInBlocks + k] = nl[k];
    }

    public string Convert(ImageBuffer imageBuffer, int blockSize)
    {
        BlockSize = blockSize;
        return Convert(imageBuffer);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
