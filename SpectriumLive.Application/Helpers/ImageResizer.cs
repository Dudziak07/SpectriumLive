using SpectriumLive.Core.Entities;
using System.Buffers;
using System.Runtime.InteropServices;

namespace SpectriumLive.Application.Helpers;

public static class ImageResizer
{
    private const double AspectRatioCorrection = 0.5;

    public static ImageBuffer Resize(ImageBuffer source, int newWidth)
    {
        if ( source.Width is 0 || source.Height is 0 )
            return source;

        double aspectRatio = (double)source.Height / source.Width;
        int newHeight = (int)( newWidth * aspectRatio * AspectRatioCorrection );
        if ( newHeight < 1 )
            newHeight = 1;

        var resizedPixels = new PixelData[newWidth * newHeight];

        var pool = ArrayPool<int>.Shared;
        int[] xMap = pool.Rent(newWidth);
        int[] yMap = pool.Rent(newHeight);

        try
        {
            for ( int x = 0; x < newWidth; x++ )
            {
                xMap[x] = Math.Min((int)Math.Round((double)x / newWidth * source.Width), source.Width - 1);
            }

            for ( int y = 0; y < newHeight; y++ )
            {
                yMap[y] = Math.Min((int)Math.Round((double)y / newHeight * source.Height), source.Height - 1);
            }

            int srcWidth = source.Width;
            PixelData[] srcPixels = source.Pixels;
            PixelData[] dstPixels = resizedPixels;
            int totalDestPixels = newWidth * newHeight;

            if (totalDestPixels < 65_536 || Environment.ProcessorCount <= 1)
            {
                var srcUint = MemoryMarshal.Cast<PixelData, uint>(srcPixels.AsSpan());
                var dstUint = MemoryMarshal.Cast<PixelData, uint>(dstPixels.AsSpan());

                for (int y = 0; y < newHeight; y++)
                {
                    int sourceRowOffset = yMap[y] * srcWidth;
                    int destRowOffset = y * newWidth;
                    for (int x = 0; x < newWidth; x++)
                    {
                        dstUint[destRowOffset + x] = srcUint[sourceRowOffset + xMap[x]];
                    }
                }
            }
            else
            {
                Parallel.For(0, newHeight, y =>
                {
                    int sourceRowOffset = yMap[y] * srcWidth;
                    int destRowOffset = y * newWidth;
                    var srcUintRow = MemoryMarshal.Cast<PixelData, uint>(srcPixels.AsSpan(sourceRowOffset, srcWidth));
                    var dstUintRow = MemoryMarshal.Cast<PixelData, uint>(dstPixels.AsSpan(destRowOffset, newWidth));

                    for (int x = 0; x < newWidth; x++)
                    {
                        dstUintRow[x] = srcUintRow[xMap[x]];
                    }
                });
            }
        }
        finally
        {
            pool.Return(xMap);
            pool.Return(yMap);
        }

        return new(newWidth, newHeight, resizedPixels);
    }
}
