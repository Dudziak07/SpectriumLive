using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Tests.TestHelpers;

internal readonly record struct ImageComparisonMetrics(
    int MaxChannelDelta,
    double MeanAbsoluteError,
    double PsnrDb);

internal static class TestImageFactory
{
    public static ImageBuffer CreateSolid(int width, int height, PixelData color)
    {
        var pixels = new PixelData[width * height];
        Array.Fill(pixels, color);
        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer CreateGradient(int width, int height)
    {
        var pixels = new PixelData[width * height];
        for (int y = 0; y < height; y++)
        {
            byte g = height <= 1 ? (byte)128 : (byte)(y * 255 / (height - 1));
            for (int x = 0; x < width; x++)
            {
                byte r = width <= 1 ? (byte)128 : (byte)(x * 255 / (width - 1));
                byte b = (byte)((r + g) / 2);
                byte a = (byte)(100 + ((x + y) % 156));
                pixels[y * width + x] = new PixelData(r, g, b, a);
            }
        }
        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer CreateCheckerboard(int width, int height, int cellSize = 4)
    {
        var pixels = new PixelData[width * height];
        int validCell = Math.Max(1, cellSize);

        for (int y = 0; y < height; y++)
        {
            int cy = y / validCell;
            for (int x = 0; x < width; x++)
            {
                int cx = x / validCell;
                bool isLight = ((cx + cy) & 1) == 0;
                byte v = isLight ? (byte)240 : (byte)15;
                byte a = (byte)(isLight ? 255 : 180);
                pixels[y * width + x] = new PixelData(v, v, v, a);
            }
        }
        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer CreateBimodal(int width, int height, byte low = 30, byte high = 220, double lowRatio = 0.4)
    {
        int total = width * height;
        int lowCount = (int)(total * lowRatio);
        var pixels = new PixelData[total];
        var rng = new Random(1337);

        for (int i = 0; i < total; i++)
        {
            int baseVal = i < lowCount ? low : high;
            byte v = (byte)Math.Clamp(baseVal + rng.Next(-10, 11), 0, 255);
            pixels[i] = new PixelData(v, v, v, 255);
        }

        // Deterministic shuffle so spatial locality doesn't skew block/parallel paths
        for (int i = total - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pixels[i], pixels[j]) = (pixels[j], pixels[i]);
        }

        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer CreateNoise(int width, int height, int seed = 42)
    {
        var pixels = new PixelData[width * height];
        var rng = new Random(seed);
        Span<byte> buf = stackalloc byte[4];

        for (int i = 0; i < pixels.Length; i++)
        {
            rng.NextBytes(buf);
            pixels[i] = new PixelData(buf[0], buf[1], buf[2], buf[3]);
        }

        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer CreateRadialTarget(int width, int height)
    {
        var pixels = new PixelData[width * height];
        double cx = (width - 1) / 2.0;
        double cy = (height - 1) / 2.0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                bool ring = ((int)(dist / 8.0) & 1) == 0;
                bool axis = Math.Abs(x - cx) < 1.5 || Math.Abs(y - cy) < 1.5;

                pixels[y * width + x] = axis
                    ? new PixelData(255, 40, 40, 255)
                    : ring
                        ? new PixelData(230, 230, 230, 255)
                        : new PixelData(30, 60, 120, 255);
            }
        }

        return new ImageBuffer(width, height, pixels);
    }

    public static ImageBuffer Clone(ImageBuffer source) =>
        new(source.Width, source.Height, (PixelData[])source.Pixels.Clone());

    public static ImageComparisonMetrics Compare(ImageBuffer expected, ImageBuffer actual, bool ignoreBorder = false, int borderOffset = 0)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            throw new ArgumentException("Image dimensions do not match.");

        int w = expected.Width;
        int h = expected.Height;
        int startX = ignoreBorder ? borderOffset : 0;
        int endX = ignoreBorder ? w - borderOffset : w;
        int startY = ignoreBorder ? borderOffset : 0;
        int endY = ignoreBorder ? h - borderOffset : h;

        int maxDelta = 0;
        long sumAbsError = 0;
        long sumSqError = 0;
        long channelSamples = 0;

        for (int y = startY; y < endY; y++)
        {
            int rowOffset = y * w;
            for (int x = startX; x < endX; x++)
            {
                var e = expected.Pixels[rowOffset + x];
                var a = actual.Pixels[rowOffset + x];

                int dr = Math.Abs(e.R - a.R);
                int dg = Math.Abs(e.G - a.G);
                int db = Math.Abs(e.B - a.B);
                int da = Math.Abs(e.A - a.A);

                maxDelta = Math.Max(maxDelta, Math.Max(Math.Max(dr, dg), Math.Max(db, da)));
                sumAbsError += dr + dg + db + da;
                sumSqError += (long)dr * dr + (long)dg * dg + (long)db * db + (long)da * da;
                channelSamples += 4;
            }
        }

        if (channelSamples == 0)
            return new ImageComparisonMetrics(0, 0.0, double.PositiveInfinity);

        double mae = (double)sumAbsError / channelSamples;
        double mse = (double)sumSqError / channelSamples;
        double psnr = mse == 0 ? double.PositiveInfinity : 10.0 * Math.Log10((255.0 * 255.0) / mse);

        return new ImageComparisonMetrics(maxDelta, mae, psnr);
    }
}

