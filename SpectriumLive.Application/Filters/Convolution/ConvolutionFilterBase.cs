using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.Buffers;

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

    public virtual void Apply(ImageBuffer imageBuffer)
    {
        var width = imageBuffer.Width;
        var height = imageBuffer.Height;
        var pixels = imageBuffer.Pixels;

        var kernel2D = Kernel;
        int kernelSize = kernel2D.GetLength(0);
        int kernelOffset = kernelSize / 2;

        if (width < kernelSize || height < kernelSize || pixels.Length < width * height)
            return;

        double[] flatKernel = new double[kernelSize * kernelSize];
        for (int ky = 0; ky < kernelSize; ky++)
            for (int kx = 0; kx < kernelSize; kx++)
                flatKernel[ky * kernelSize + kx] = kernel2D[ky, kx];

        double factor = Factor;
        double bias = Bias;
        bool useAbs = UseAbsoluteValue;

        var (bandCount, seams) = ConvolutionSimdHelper.PrepareBands(pixels, width, height, kernelOffset,
            out int interiorStart, out int interiorRows, out int rowsPerBand, out int remainderRows);
        if (bandCount == 0)
            return;

        try
        {
            if (bandCount == 1)
            {
                ProcessGenericBand(pixels, null, width, kernelSize, kernelOffset, flatKernel, factor, bias, useAbs, 0, 1, interiorStart, interiorStart + interiorRows);
            }
            else
            {
                Parallel.For(0, bandCount, bandIndex =>
                {
                    ConvolutionSimdHelper.GetBandBounds(bandIndex, interiorStart, rowsPerBand, remainderRows, out int y0, out int y1);
                    ProcessGenericBand(pixels, seams, width, kernelSize, kernelOffset, flatKernel, factor, bias, useAbs, bandIndex, bandCount, y0, y1);
                });
            }
        }
        finally
        {
            ConvolutionSimdHelper.ReturnSeams(seams);
        }
    }

    private static void ProcessGenericBand(
        PixelData[] pixels,
        PixelData[][]? seams,
        int width,
        int kernelSize,
        int kernelOffset,
        double[] flatKernel,
        double factor,
        double bias,
        bool useAbs,
        int bandIndex,
        int bandCount,
        int y0,
        int y1)
    {
        PixelData[] ring = ArrayPool<PixelData>.Shared.Rent(kernelSize * width);
        try
        {
            for (int i = 0; i < kernelSize - 1; i++)
            {
                var srcRow = ConvolutionSimdHelper.GetSourceRow(pixels, seams, width, kernelOffset, bandIndex, bandCount, y0, y1, y0 - kernelOffset + i);
                srcRow.CopyTo(ring.AsSpan(i * width, width));
            }

            int nextSlot = kernelSize - 1;
            int topSlot = 0;

            for (int y = y0; y < y1; y++)
            {
                var nextSrcRow = ConvolutionSimdHelper.GetSourceRow(pixels, seams, width, kernelOffset, bandIndex, bandCount, y0, y1, y + kernelOffset);
                nextSrcRow.CopyTo(ring.AsSpan(nextSlot * width, width));

                var dstRow = pixels.AsSpan(y * width, width);

                for (int x = kernelOffset; x < width - kernelOffset; x++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;

                    for (int ky = 0; ky < kernelSize; ky++)
                    {
                        int slot = topSlot + ky;
                        if (slot >= kernelSize) slot -= kernelSize;
                        int ringOffset = slot * width + x - kernelOffset;
                        int kOffset = ky * kernelSize;

                        for (int kx = 0; kx < kernelSize; kx++)
                        {
                            var p = ring[ringOffset + kx];
                            double k = flatKernel[kOffset + kx];
                            sumR += p.R * k;
                            sumG += p.G * k;
                            sumB += p.B * k;
                        }
                    }

                    if (useAbs)
                    {
                        sumR = Math.Abs(sumR);
                        sumG = Math.Abs(sumG);
                        sumB = Math.Abs(sumB);
                    }

                    byte finalR = (byte)Math.Clamp(factor * sumR + bias, byte.MinValue, byte.MaxValue);
                    byte finalG = (byte)Math.Clamp(factor * sumG + bias, byte.MinValue, byte.MaxValue);
                    byte finalB = (byte)Math.Clamp(factor * sumB + bias, byte.MinValue, byte.MaxValue);
                    dstRow[x] = new PixelData(finalR, finalG, finalB, dstRow[x].A);
                }

                nextSlot++;
                if (nextSlot == kernelSize) nextSlot = 0;
                topSlot++;
                if (topSlot == kernelSize) topSlot = 0;
            }
        }
        finally
        {
            ArrayPool<PixelData>.Shared.Return(ring);
        }
    }
}
