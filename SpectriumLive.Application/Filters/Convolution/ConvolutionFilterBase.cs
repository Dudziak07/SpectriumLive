using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters.Convolution;
public abstract class ConvolutionFilterBase : IFilter
{
    public abstract string Name { get; }
    public ImageProcessorGroup Group => ImageProcessorGroup.Convolution;
    public virtual bool HasConfigurableProperties => false;
    protected abstract double[,] Kernel { get; }
    protected virtual double Factor { get; } = 1.0;
    protected virtual double Bias { get; } = 0.0;
    protected virtual bool UseAbsoluteValue { get; } = false;

    public void Apply(ImageBuffer imageBuffer)
    {
        var width = imageBuffer.Width;
        var height = imageBuffer.Height;
        var sourcePixels = (PixelData[])imageBuffer.Pixels.Clone();
        var destinationPixels = imageBuffer.Pixels;

        int kernelSize = Kernel.GetLength(0);
        int kernelOffset = kernelSize / 2;

        Parallel.For(kernelOffset, height - kernelOffset, y =>
        {
            for ( int x = kernelOffset; x < width - kernelOffset; x++ )
            {
                double sumR = 0, sumG = 0, sumB = 0;

                for ( int ky = 0; ky < kernelSize; ky++ )
                {
                    for ( int kx = 0; kx < kernelSize; kx++ )
                    {
                        int pixelX = x + kx - kernelOffset;
                        int pixelY = y + ky - kernelOffset;

                        var p = sourcePixels[pixelY * width + pixelX];
                        var k = Kernel[ky, kx];

                        sumR += p.R * k;
                        sumG += p.G * k;
                        sumB += p.B * k;
                    }
                }

                if ( UseAbsoluteValue )
                {
                    sumR = Math.Abs(sumR);
                    sumG = Math.Abs(sumG);
                    sumB = Math.Abs(sumB);
                }

                byte finalR = (byte)Math.Max(0, Math.Min(255, Factor * sumR + Bias));
                byte finalG = (byte)Math.Max(0, Math.Min(255, Factor * sumG + Bias));
                byte finalB = (byte)Math.Max(0, Math.Min(255, Factor * sumB + Bias));

                destinationPixels[y * width + x] = new PixelData(finalR, finalG, finalB, sourcePixels[y * width + x].A);
            }
        });
    }
}
