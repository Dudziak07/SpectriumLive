using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Services;
using SpectriumLive.Application.Tests.TestHelpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Quality;

public class NewFiltersOracleTests
{
    public static TheoryData<int, int> OddAndDiverseResolutions => new()
    {
        { 1, 1 },
        { 2, 2 },
        { 7, 5 },
        { 17, 13 },
        { 63, 31 },
        { 127, 65 },
        { 319, 241 }
    };

    public static TheoryData<int, int> OracleResolutions => new()
    {
        { 17, 13 },
        { 127, 65 },
        { 640, 480 }
    };

    private static IEnumerable<IFilter> CreateNewFilters()
    {
        yield return new BrightnessContrastFilter { Brightness = 25, Contrast = 1.35 };
        yield return new SharpenFilter();
        yield return new SobelEdgeFilter();
    }

    [Fact]
    public void NewFilters_OnEmptyAndTinyBuffers_DoNotThrow()
    {
        foreach (var filter in CreateNewFilters())
        {
            var empty = new ImageBuffer(0, 0, []);
            filter.Apply(empty);
            Assert.Empty(empty.Pixels);

            var tiny1 = TestImageFactory.CreateNoise(1, 1, seed: 11);
            var tiny1Copy = TestImageFactory.Clone(tiny1);
            filter.Apply(tiny1);
            if (filter is SharpenFilter or SobelEdgeFilter)
                Assert.Equal(tiny1Copy.Pixels, tiny1.Pixels);

            var tiny2 = TestImageFactory.CreateNoise(2, 2, seed: 12);
            var tiny2Copy = TestImageFactory.Clone(tiny2);
            filter.Apply(tiny2);
            if (filter is SharpenFilter or SobelEdgeFilter)
                Assert.Equal(tiny2Copy.Pixels, tiny2.Pixels);
        }
    }

    [Theory]
    [MemberData(nameof(OddAndDiverseResolutions))]
    public void NewFilters_PreserveAlphaChannel(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height, seed: 123);

        foreach (var filter in CreateNewFilters())
        {
            var actual = TestImageFactory.Clone(source);
            filter.Apply(actual);

            for (int i = 0; i < source.Pixels.Length; i++)
            {
                Assert.Equal(source.Pixels[i].A, actual.Pixels[i].A);
            }
        }
    }

    [Theory]
    [MemberData(nameof(OddAndDiverseResolutions))]
    public void NewFilters_AreDeterministic_AcrossRepeatedRuns(int width, int height)
    {
        var source = TestImageFactory.CreateGradient(width, height);

        foreach (var filter in CreateNewFilters())
        {
            var run1 = TestImageFactory.Clone(source);
            var run2 = TestImageFactory.Clone(source);

            filter.Apply(run1);
            filter.Apply(run2);

            Assert.Equal(run1.Pixels, run2.Pixels);
        }
    }

    [Fact]
    public void BrightnessContrastFilter_DefaultValues_AreNoOp_AndPropertiesClamp()
    {
        var source = TestImageFactory.CreateNoise(65, 33, seed: 5);
        var actual = TestImageFactory.Clone(source);
        var filter = new BrightnessContrastFilter();

        filter.Apply(actual);
        Assert.Equal(source.Pixels, actual.Pixels);

        var changedProps = new List<string?>();
        filter.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

        filter.Brightness = 999;
        Assert.Equal(255, filter.Brightness);
        filter.Brightness = -999;
        Assert.Equal(-255, filter.Brightness);
        filter.Contrast = 10.0;
        Assert.Equal(3.0, filter.Contrast);
        filter.Contrast = -2.0;
        Assert.Equal(0.0, filter.Contrast);

        Assert.Contains(nameof(BrightnessContrastFilter.Brightness), changedProps);
        Assert.Contains(nameof(BrightnessContrastFilter.Contrast), changedProps);
    }

    [Theory]
    [InlineData(17, 13, 35, 1.4)]
    [InlineData(127, 65, -45, 0.65)]
    [InlineData(640, 480, 20, 1.8)]
    public void BrightnessContrastFilter_MatchesExactReferenceOracle(int width, int height, int brightness, double contrast)
    {
        var source = TestImageFactory.CreateNoise(width, height, seed: 42);
        var expected = TestImageFactory.Clone(source);
        for (int i = 0; i < expected.Pixels.Length; i++)
        {
            var p = expected.Pixels[i];
            byte r = (byte)Math.Clamp((int)Math.Round((p.R - 128) * contrast + 128 + brightness), 0, 255);
            byte g = (byte)Math.Clamp((int)Math.Round((p.G - 128) * contrast + 128 + brightness), 0, 255);
            byte b = (byte)Math.Clamp((int)Math.Round((p.B - 128) * contrast + 128 + brightness), 0, 255);
            expected.Pixels[i] = new PixelData(r, g, b, p.A);
        }

        var actual = TestImageFactory.Clone(source);
        new BrightnessContrastFilter { Brightness = brightness, Contrast = contrast }.Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual);
        Assert.Equal(0, metrics.MaxChannelDelta);
    }

    [Theory]
    [MemberData(nameof(OracleResolutions))]
    public void SharpenFilter_InteriorMatchesExactReferenceOracle_AndPreservesBorders(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height, seed: 77);
        var expected = ReferenceSharpen3x3(source);

        var actual = TestImageFactory.Clone(source);
        new SharpenFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual, ignoreBorder: true, borderOffset: 1);
        Assert.Equal(0, metrics.MaxChannelDelta);
        AssertBordersPreserved(source, actual);
    }

    [Theory]
    [MemberData(nameof(OracleResolutions))]
    public void SobelEdgeFilter_InteriorMatchesReferenceOracle_AndPreservesBorders(int width, int height)
    {
        var source = TestImageFactory.CreateCheckerboard(width, height, cellSize: 5);
        var expected = ReferenceSobel3x3(source);

        var actual = TestImageFactory.Clone(source);
        new SobelEdgeFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual, ignoreBorder: true, borderOffset: 1);
        Assert.True(metrics.MaxChannelDelta <= 1, $"Sobel max delta {metrics.MaxChannelDelta} exceeded 1 LSB.");
        AssertBordersPreserved(source, actual);

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                var p = actual.Pixels[y * width + x];
                Assert.Equal(p.R, p.G);
                Assert.Equal(p.R, p.B);
            }
        }
    }

    [Fact]
    public void ImageProcessingService_RegistersNewFilters()
    {
        var processors = new ImageProcessingService().GetAvailableProcessors().ToList();
        Assert.Contains(processors, p => p is BrightnessContrastFilter);
        Assert.Contains(processors, p => p is SharpenFilter);
        Assert.Contains(processors, p => p is SobelEdgeFilter);
    }

    private static ImageBuffer ReferenceSharpen3x3(ImageBuffer source)
    {
        int w = source.Width;
        int h = source.Height;
        var result = TestImageFactory.Clone(source);

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                var c = source.Pixels[y * w + x];
                var t = source.Pixels[(y - 1) * w + x];
                var b = source.Pixels[(y + 1) * w + x];
                var l = source.Pixels[y * w + (x - 1)];
                var r = source.Pixels[y * w + (x + 1)];

                byte rr = (byte)Math.Clamp(5 * c.R - (t.R + b.R + l.R + r.R), 0, 255);
                byte gg = (byte)Math.Clamp(5 * c.G - (t.G + b.G + l.G + r.G), 0, 255);
                byte bb = (byte)Math.Clamp(5 * c.B - (t.B + b.B + l.B + r.B), 0, 255);
                result.Pixels[y * w + x] = new PixelData(rr, gg, bb, c.A);
            }
        }

        return result;
    }

    private static ImageBuffer ReferenceSobel3x3(ImageBuffer source)
    {
        int w = source.Width;
        int h = source.Height;
        var result = TestImageFactory.Clone(source);
        var gray = new int[w * h];

        for (int i = 0; i < source.Pixels.Length; i++)
        {
            var p = source.Pixels[i];
            gray[i] = (int)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
        }

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                int tl = gray[(y - 1) * w + (x - 1)];
                int t = gray[(y - 1) * w + x];
                int tr = gray[(y - 1) * w + (x + 1)];
                int l = gray[y * w + (x - 1)];
                int r = gray[y * w + (x + 1)];
                int bl = gray[(y + 1) * w + (x - 1)];
                int b = gray[(y + 1) * w + x];
                int br = gray[(y + 1) * w + (x + 1)];

                int gx = -tl + tr - 2 * l + 2 * r - bl + br;
                int gy = -tl - 2 * t - tr + bl + 2 * b + br;
                byte mag = (byte)Math.Clamp((int)Math.Sqrt(gx * gx + gy * gy), 0, 255);

                byte a = source.Pixels[y * w + x].A;
                result.Pixels[y * w + x] = new PixelData(mag, mag, mag, a);
            }
        }

        return result;
    }

    private static void AssertBordersPreserved(ImageBuffer expected, ImageBuffer actual)
    {
        int w = expected.Width;
        int h = expected.Height;
        for (int x = 0; x < w; x++)
        {
            Assert.Equal(expected.Pixels[x], actual.Pixels[x]);
            Assert.Equal(expected.Pixels[(h - 1) * w + x], actual.Pixels[(h - 1) * w + x]);
        }

        for (int y = 0; y < h; y++)
        {
            Assert.Equal(expected.Pixels[y * w], actual.Pixels[y * w]);
            Assert.Equal(expected.Pixels[y * w + (w - 1)], actual.Pixels[y * w + (w - 1)]);
        }
    }
}
