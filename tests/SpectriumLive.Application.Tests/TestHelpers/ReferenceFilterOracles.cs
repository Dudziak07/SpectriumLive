using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Tests.TestHelpers;

internal static class ReferenceFilterOracles
{
    public static byte ToGrayscale(PixelData p) =>
        (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);

    public static PixelData ApplyNegativePixel(PixelData p) =>
        new((byte)(255 - p.R), (byte)(255 - p.G), (byte)(255 - p.B), p.A);

    public static PixelData ApplyGrayscalePixel(PixelData p)
    {
        byte g = ToGrayscale(p);
        return new PixelData(g, g, g, p.A);
    }

    public static PixelData ApplySepiaPixel(PixelData p)
    {
        byte r = (byte)Math.Min(255, 0.393 * p.R + 0.769 * p.G + 0.189 * p.B);
        byte g = (byte)Math.Min(255, 0.349 * p.R + 0.686 * p.G + 0.168 * p.B);
        byte b = (byte)Math.Min(255, 0.272 * p.R + 0.534 * p.G + 0.131 * p.B);
        return new PixelData(r, g, b, p.A);
    }

    public static ImageBuffer ReferenceNegative(ImageBuffer source) =>
        MapPixels(source, ApplyNegativePixel);

    public static ImageBuffer ReferenceGrayscale(ImageBuffer source) =>
        MapPixels(source, ApplyGrayscalePixel);

    public static ImageBuffer ReferenceSepia(ImageBuffer source) =>
        MapPixels(source, ApplySepiaPixel);

    public static ImageBuffer ReferenceGaussian2D(ImageBuffer source, int size, double sigma)
    {
        var dst = TestImageFactory.Clone(source);
        var kernel = new double[size, size];
        int center = size / 2;
        double sigma2 = sigma * sigma;
        double norm = 1.0 / (2.0 * Math.PI * sigma2);
        double sum = 0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - center;
                int dy = y - center;
                double v = norm * Math.Exp(-(dx * dx + dy * dy) / (2.0 * sigma2));
                kernel[y, x] = v;
                sum += v;
            }
        }

        double factor = 1.0 / sum;
        int w = source.Width;
        int h = source.Height;

        Parallel.For(center, Math.Max(center, h - center), y =>
        {
            for (int x = center; x < w - center; x++)
            {
                double r = 0, g = 0, b = 0;
                for (int ky = 0; ky < size; ky++)
                {
                    int py = (y + ky - center) * w;
                    for (int kx = 0; kx < size; kx++)
                    {
                        var p = source.Pixels[py + (x + kx - center)];
                        double k = kernel[ky, kx];
                        r += p.R * k;
                        g += p.G * k;
                        b += p.B * k;
                    }
                }

                var orig = source.Pixels[y * w + x];
                dst.Pixels[y * w + x] = new PixelData(
                    (byte)Math.Clamp(r * factor, 0, 255),
                    (byte)Math.Clamp(g * factor, 0, 255),
                    (byte)Math.Clamp(b * factor, 0, 255),
                    orig.A);
            }
        });

        return dst;
    }

    public static ImageBuffer ReferenceBoxBlur3x3(ImageBuffer source)
    {
        var dst = TestImageFactory.Clone(source);
        int w = source.Width;
        int h = source.Height;

        Parallel.For(1, Math.Max(1, h - 1), y =>
        {
            for (int x = 1; x < w - 1; x++)
            {
                int r = 0, g = 0, b = 0;
                for (int ky = -1; ky <= 1; ky++)
                {
                    int py = (y + ky) * w;
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        var p = source.Pixels[py + (x + kx)];
                        r += p.R;
                        g += p.G;
                        b += p.B;
                    }
                }

                var orig = source.Pixels[y * w + x];
                dst.Pixels[y * w + x] = new PixelData((byte)(r / 9.0), (byte)(g / 9.0), (byte)(b / 9.0), orig.A);
            }
        });

        return dst;
    }

    public static ImageBuffer ReferenceEdgeDetection3x3(ImageBuffer source)
    {
        var dst = TestImageFactory.Clone(source);
        int w = source.Width;
        int h = source.Height;
        int[,] k = { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } };

        Parallel.For(1, Math.Max(1, h - 1), y =>
        {
            for (int x = 1; x < w - 1; x++)
            {
                int r = 0, g = 0, b = 0;
                for (int ky = 0; ky < 3; ky++)
                {
                    int py = (y + ky - 1) * w;
                    for (int kx = 0; kx < 3; kx++)
                    {
                        var p = source.Pixels[py + (x + kx - 1)];
                        int wgt = k[ky, kx];
                        r += p.R * wgt;
                        g += p.G * wgt;
                        b += p.B * wgt;
                    }
                }

                var orig = source.Pixels[y * w + x];
                dst.Pixels[y * w + x] = new PixelData(
                    (byte)Math.Clamp(Math.Abs(r), 0, 255),
                    (byte)Math.Clamp(Math.Abs(g), 0, 255),
                    (byte)Math.Clamp(Math.Abs(b), 0, 255),
                    orig.A);
            }
        });

        return dst;
    }

    private static ImageBuffer MapPixels(ImageBuffer source, Func<PixelData, PixelData> map)
    {
        var dst = TestImageFactory.Clone(source);
        Parallel.For(0, dst.Pixels.Length, i => dst.Pixels[i] = map(dst.Pixels[i]));
        return dst;
    }
}

