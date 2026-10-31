using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters;

public class FisheyeFilter(double strength = 1.0) : IFilter, INotifyPropertyChanged
{
    public string Name => "Fisheye";
    public ImageProcessorGroup Group => ImageProcessorGroup.Distortion;
    public bool HasConfigurableProperties => true;

    public double Strength
    {
        get => strength;
        set
        {
            if ( strength != value )
            {
                strength = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Strength)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Apply(ImageBuffer imageBuffer)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;

        var source = imageBuffer.Pixels;
        var destination = new PixelData[width * height];

        double centerX = ( width - 1 ) / 2.0;
        double centerY = ( height - 1 ) / 2.0;
        double radius = Math.Min(centerX, centerY);

        Parallel.For(0, height, y =>
        {
            double dy = ( y - centerY ) / radius;
            double dy2 = dy * dy;

            for ( int x = 0; x < width; x++ )
            {
                double dx = ( x - centerX ) / radius;
                double r = Math.Sqrt(dx * dx + dy2);

                if ( r < 1.0 )
                {
                    double srcX, srcY;

                    if ( r > 1e-9 )
                    {
                        double distortedR = Strength >= 0
                            ? Math.Pow(r, 1 + Strength)
                            : Math.Pow(r, 1.0 / ( 1 - Strength ));

                        double scale = ( distortedR / r ) * radius;
                        srcX = centerX + scale * dx;
                        srcY = centerY + scale * dy;
                    }
                    else
                    {
                        srcX = centerX;
                        srcY = centerY;
                    }

                    int ix = Math.Clamp((int)( srcX + 0.5 ), 0, width - 1);
                    int iy = Math.Clamp((int)( srcY + 0.5 ), 0, height - 1);

                    destination[y * width + x] = source[iy * width + ix];
                }
                else
                {
                    destination[y * width + x] = new PixelData(0, 0, 0, 255);
                }
            }
        });

        Array.Copy(destination, imageBuffer.Pixels, destination.Length);
    }
}
