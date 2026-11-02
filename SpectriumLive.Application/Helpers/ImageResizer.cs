using SpectriumLive.Core.Entities;

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

        int[] xMap = new int[newWidth];
        for ( int x = 0; x < newWidth; x++ )
        {
            xMap[x] = Math.Min((int)Math.Round((double)x / newWidth * source.Width), source.Width - 1);
        }

        Parallel.For(0, newHeight, y =>
        {
            int sourceY = Math.Min((int)Math.Round((double)y / newHeight * source.Height), source.Height - 1);
            int sourceRowOffset = sourceY * source.Width;
            int destRowOffset = y * newWidth;

            for ( int x = 0; x < newWidth; x++ )
            {
                resizedPixels[destRowOffset + x] = source.Pixels[sourceRowOffset + xMap[x]];
            }
        });

        return new(newWidth, newHeight, resizedPixels);
    }
}
