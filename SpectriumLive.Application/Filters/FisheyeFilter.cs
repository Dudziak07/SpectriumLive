using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;
public class FisheyeFilter : IFilter
{
    public string Name => "Fisheye";

    public void Apply(ImageBuffer imageBuffer)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;
        var originalPixels = (PixelData[])imageBuffer.Pixels.Clone();

        double centerX = width / 2.0;
        double centerY = height / 2.0;
        double radius = Math.Min(centerX, centerY);

        Parallel.For(0, height, y =>
        {
            for ( int x = 0; x < width; x++ )
            {
                double dx = ( x - centerX ) / radius;
                double dy = ( y - centerY ) / radius;
                double r = Math.Sqrt(dx * dx + dy * dy);

                if ( r <= 1.0 )
                {
                    double theta = Math.Atan2(dy, dx);

                    double newR = Math.Pow(r, 2);

                    double srcX = centerX + newR * radius * Math.Cos(theta);
                    double srcY = centerY + newR * radius * Math.Sin(theta);

                    int ix = (int)Math.Clamp(srcX, 0, width - 1);
                    int iy = (int)Math.Clamp(srcY, 0, height - 1);

                    imageBuffer.Pixels[y * width + x] = originalPixels[iy * width + ix];
                }
                else
                {
                    imageBuffer.Pixels[y * width + x] = new PixelData(0, 0, 0, 255);
                }
            }
        });
    }
}
