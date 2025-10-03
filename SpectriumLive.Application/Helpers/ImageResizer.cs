using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Helpers;
public class ImageResizer
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

        Parallel.For(0, newHeight, y =>
        {
            for ( int x = 0; x < newWidth; x++ )
            {
                int sourceX = (int)Math.Round((double)x / newWidth * source.Width);
                int sourceY = (int)Math.Round((double)y / newHeight * source.Height);

                sourceX = Math.Min(sourceX, source.Width - 1);
                sourceY = Math.Min(sourceY, source.Height - 1);

                resizedPixels[y * newWidth + x] = source.Pixels[sourceY * source.Width + sourceX];
            }
        });
        return new(newWidth, newHeight, resizedPixels);
    }
}
