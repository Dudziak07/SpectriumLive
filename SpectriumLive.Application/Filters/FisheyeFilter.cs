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

        var source = (PixelData[])imageBuffer.Pixels.Clone();
        var destination = new PixelData[width * height];

        double centerX = ( width - 1 ) / 2.0;
        double centerY = ( height - 1 ) / 2.0;
        double radius = Math.Min(centerX, centerY);

        Parallel.For(0, height, y =>
        {
            for ( int x = 0; x < width; x++ )
            {
                double dx = ( x - centerX ) / radius;
                double dy = ( y - centerY ) / radius;
                double r = Math.Sqrt(dx * dx + dy * dy);

                if ( r < 1.0 )
                {
                    double theta = Math.Atan2(dy, dx);

                    double distortedR = Strength >= 0
                        ? Math.Pow(r, 1 + Strength)
                        : Math.Pow(r, 1.0 / ( 1 - Strength ));

                    double srcX = centerX + distortedR * radius * Math.Cos(theta);
                    double srcY = centerY + distortedR * radius * Math.Sin(theta);

                    int ix = Math.Clamp((int)(srcX + 0.5), 0, width - 1);
                    int iy = Math.Clamp((int)(srcY + 0.5), 0, height - 1);

                    destination[y * width + x] = source[iy * width + ix];
                }
                else
                {
                    destination[y * width + x] = new PixelData(0, 0, 0, 255);
                }
            }
        });

        var target = imageBuffer.Pixels;
        for ( int i = 0; i < destination.Length; i++ )
        {
            target[i] = destination[i];
        }
    }
}
