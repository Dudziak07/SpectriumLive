using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Buffers;
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

        if (width <= 0 || height <= 0 || source.Length < width * height)
            return;

        double centerX = ( width - 1 ) / 2.0;
        double centerY = ( height - 1 ) / 2.0;
        double radius = Math.Min(centerX, centerY);

        if (radius <= 0)
            return;

        double invRadius = 1.0 / radius;
        double exponent = Strength >= 0
            ? 1.0 + Strength
            : 1.0 / ( 1.0 - Strength );

        bool isQuadratic = Math.Abs(exponent - 2.0) < 1e-9;
        bool isLinear = Math.Abs(exponent - 1.0) < 1e-9;
        double expMinus1 = exponent - 1.0;

        double[] dxArray = ArrayPool<double>.Shared.Rent(width);
        double[] dx2Array = ArrayPool<double>.Shared.Rent(width);

        int totalPixels = width * height;
        PixelData[] destination = ArrayPool<PixelData>.Shared.Rent(totalPixels);

        try
        {
            for (int x = 0; x < width; x++)
            {
                double dx = (x - centerX) * invRadius;
                dxArray[x] = dx;
                dx2Array[x] = dx * dx;
            }

            PixelData clearPixel = new(0, 0, 0, 255);

            Parallel.For(0, height, y =>
            {
                double dy = ( y - centerY ) * invRadius;
                double dy2 = dy * dy;
                int rowOffset = y * width;
                var dstRow = destination.AsSpan(rowOffset, width);

                if (dy2 >= 1.0)
                {
                    dstRow.Fill(clearPixel);
                    return;
                }

                double maxDx = Math.Sqrt(1.0 - dy2);
                int xStart = (int)Math.Ceiling(centerX - maxDx * radius);
                int xEnd = (int)Math.Floor(centerX + maxDx * radius);

                xStart = Math.Clamp(xStart, 0, width);
                xEnd = Math.Clamp(xEnd, -1, width - 1);

                if (xStart > 0)
                    dstRow.Slice(0, xStart).Fill(clearPixel);

                if (xEnd + 1 < width)
                    dstRow.Slice(xEnd + 1, width - (xEnd + 1)).Fill(clearPixel);

                for (int x = xStart; x <= xEnd; x++)
                {
                    double dx = dxArray[x];
                    double r = Math.Sqrt(dx2Array[x] + dy2);

                    if (r < 1.0)
                    {
                        double srcX, srcY;

                        if (r > 1e-9)
                        {
                            double scale = isQuadratic
                                ? r * radius
                                : isLinear
                                    ? radius
                                    : Math.Pow(r, expMinus1) * radius;

                            srcX = centerX + scale * dx;
                            srcY = centerY + scale * dy;
                        }
                        else
                        {
                            srcX = centerX;
                            srcY = centerY;
                        }

                        int ix = Math.Clamp((int)(srcX + 0.5), 0, width - 1);
                        int iy = Math.Clamp((int)(srcY + 0.5), 0, height - 1);

                        dstRow[x] = source[iy * width + ix];
                    }
                    else
                    {
                        dstRow[x] = clearPixel;
                    }
                }
            });

            Array.Copy(destination, imageBuffer.Pixels, totalPixels);
        }
        finally
        {
            ArrayPool<PixelData>.Shared.Return(destination);
            ArrayPool<double>.Shared.Return(dxArray);
            ArrayPool<double>.Shared.Return(dx2Array);
        }
    }
}
