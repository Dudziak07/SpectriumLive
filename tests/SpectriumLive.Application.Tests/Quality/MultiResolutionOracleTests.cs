using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.Tests.TestHelpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Quality;

public class MultiResolutionOracleTests
{
    public static TheoryData<string, int, int> StandardAndPowerOfTwoResolutions => new()
    {
        // Powers of 2 (small to 2048x2048)
        { "Pow2_16x16", 16, 16 },
        { "Pow2_32x32", 32, 32 },
        { "Pow2_64x64", 64, 64 },
        { "Pow2_128x128", 128, 128 },
        { "Pow2_256x256", 256, 256 },
        { "Pow2_512x512", 512, 512 },
        { "Pow2_1024x1024", 1024, 1024 },
        { "Pow2_2048x2048", 2048, 2048 },

        // Standard video / display / scientific / photographic formats
        { "SD_640x480", 640, 480 },
        { "Thermal_640x512", 640, 512 },
        { "Kodak_768x512", 768, 512 },
        { "HD_1280x720", 1280, 720 },
        { "FHD_1920x1080", 1920, 1080 },
        { "WUXGA_1920x1200", 1920, 1200 },
        { "DCI2K_2048x1080", 2048, 1080 },
        { "QHD2K_2560x1440", 2560, 1440 },
        { "WQXGA_2560x1600", 2560, 1600 },
        { "UHD3K_2880x1620", 2880, 1620 },
        { "UHD4K_3840x2160", 3840, 2160 },

        // Portrait / smartphone & asymmetric vertical formats (catch width/height swap & stride bugs)
        { "NarrowStrip_31x1023", 31, 1023 },
        { "Portrait_Kodak_512x768", 512, 768 },
        { "Portrait_PhoneFHD_1080x1920", 1080, 1920 },
        { "Portrait_Phone20_9_1080x2400", 1080, 2400 },
        { "Portrait_PhonePro_1290x2796", 1290, 2796 },
        { "Portrait_4K_2160x3840", 2160, 3840 }
    };

    [Theory]
    [MemberData(nameof(StandardAndPowerOfTwoResolutions))]
    public void ColorAndBinaryFilters_AcrossResolutions_MatchReferenceAndPreserveAlpha(string label, int width, int height)
    {
        _ = label;
        var source = TestImageFactory.CreateGradient(width, height);

        // Negative (exact)
        var negativeActual = TestImageFactory.Clone(source);
        new NegativeFilter().Apply(negativeActual);
        var negativeExpected = ReferenceFilterOracles.ReferenceNegative(source);
        Assert.Equal(0, TestImageFactory.Compare(negativeExpected, negativeActual).MaxChannelDelta);

        // Grayscale (<= 1 LSB)
        var grayActual = TestImageFactory.Clone(source);
        new GrayscaleFilter().Apply(grayActual);
        var grayExpected = ReferenceFilterOracles.ReferenceGrayscale(source);
        var grayMetrics = TestImageFactory.Compare(grayExpected, grayActual);
        Assert.True(grayMetrics.MaxChannelDelta <= 1, $"Grayscale {width}x{height} delta: {grayMetrics.MaxChannelDelta}");

        // Sepia (<= 1 LSB)
        var sepiaActual = TestImageFactory.Clone(source);
        new SepiaFilter().Apply(sepiaActual);
        var sepiaExpected = ReferenceFilterOracles.ReferenceSepia(source);
        var sepiaMetrics = TestImageFactory.Compare(sepiaExpected, sepiaActual);
        Assert.True(sepiaMetrics.MaxChannelDelta <= 1, $"Sepia {width}x{height} delta: {sepiaMetrics.MaxChannelDelta}");

        // Binary Otsu
        var otsuActual = TestImageFactory.Clone(source);
        var otsu = new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
        otsu.Apply(otsuActual);
        Assert.InRange(otsu.CalculatedThreshold, 0, 255);
        Assert.Equal(source.Pixels[0].A, otsuActual.Pixels[0].A);
        Assert.Equal(source.Pixels[^1].A, otsuActual.Pixels[^1].A);
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(128, 128)]
    [InlineData(31, 1023)]
    [InlineData(768, 512)]
    [InlineData(1920, 1200)]
    [InlineData(1080, 1920)]
    [InlineData(1080, 2400)]
    public void ConvolutionFisheyeAndAscii_AcrossResolutions_ProduceValidOutputs(int width, int height)
    {
        var source = TestImageFactory.CreateCheckerboard(width, height, cellSize: 4);

        var gaussian = TestImageFactory.Clone(source);
        new GaussianBlurFilter { KernelSize = 5, Sigma = 1.4 }.Apply(gaussian);
        var gaussianOracle = ReferenceFilterOracles.ReferenceGaussian2D(source, 5, 1.4);
        var gaussianMetrics = TestImageFactory.Compare(gaussianOracle, gaussian, ignoreBorder: true, borderOffset: 2);
        Assert.True(gaussianMetrics.MaxChannelDelta <= 1);
        Assert.True(gaussianMetrics.PsnrDb >= 48.0 || double.IsPositiveInfinity(gaussianMetrics.PsnrDb));

        var box = TestImageFactory.Clone(source);
        new BoxBlurFilter().Apply(box);
        var boxOracle = ReferenceFilterOracles.ReferenceBoxBlur3x3(source);
        Assert.True(TestImageFactory.Compare(boxOracle, box, ignoreBorder: true, borderOffset: 1).MaxChannelDelta <= 1);

        var edge = TestImageFactory.Clone(source);
        new EdgeDetectionFilter().Apply(edge);
        var edgeOracle = ReferenceFilterOracles.ReferenceEdgeDetection3x3(source);
        Assert.True(TestImageFactory.Compare(edgeOracle, edge, ignoreBorder: true, borderOffset: 1).MaxChannelDelta <= 1);

        var fisheye = TestImageFactory.Clone(source);
        new FisheyeFilter(1.0).Apply(fisheye);
        int centerIdx = (height / 2) * width + (width / 2);
        Assert.Equal(source.Pixels[centerIdx].A, fisheye.Pixels[centerIdx].A);

        var ascii = new AsciiConverter { AsciiMode = AsciiMode.Simple, BlockSize = 4 }.Convert(source);
        Assert.NotEmpty(ascii);

        var resized = ImageResizer.Resize(source, Math.Max(8, width / 2));
        Assert.Equal(Math.Max(8, width / 2), resized.Width);
        Assert.True(resized.Height > 0);
    }
}

[CollectionDefinition("ExtremeMemory", DisableParallelization = true)]
public sealed class ExtremeMemoryCollection;

[Collection("ExtremeMemory")]
[Trait("Category", "Extreme")]
public class ExtremeResolutionOracleTests
{
    public static TheoryData<string, int, int> ExtremeResolutions => new()
    {
        { "Pow2_4096x4096", 4096, 4096 },
        { "Photo_24MP_6000x4000", 6000, 4000 },
        { "UHD8K_7680x4320", 7680, 4320 },
        { "Photo_45MP_8192x5464", 8192, 5464 },
        { "Pow2_8192x8192", 8192, 8192 },
        { "UHD16K_15360x8640", 15360, 8640 },
        { "Pow2_16384x16384", 16384, 16384 }
    };

    [Theory]
    [MemberData(nameof(ExtremeResolutions))]
    public void AllProcessors_OnExtremeResolutions_ExecuteCleanlyAndMatchSampledOracle(string label, int width, int height)
    {
        _ = label;
        var source = TestImageFactory.CreateGradient(width, height);
        int[] sampleIndices =
        [
            0,
            width / 2,
            width - 1,
            (height / 2) * width + (width / 2),
            (height - 1) * width,
            (width * height) - 1
        ];

        // 1. Negative
        var work = TestImageFactory.Clone(source);
        new NegativeFilter().Apply(work);
        foreach (int idx in sampleIndices)
            Assert.Equal(ReferenceFilterOracles.ApplyNegativePixel(source.Pixels[idx]), work.Pixels[idx]);

        // 2. Grayscale
        Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
        new GrayscaleFilter().Apply(work);
        foreach (int idx in sampleIndices)
        {
            var expected = ReferenceFilterOracles.ApplyGrayscalePixel(source.Pixels[idx]);
            Assert.InRange(Math.Abs(expected.R - work.Pixels[idx].R), 0, 1);
            Assert.Equal(source.Pixels[idx].A, work.Pixels[idx].A);
        }

        // 3. Binary Otsu
        Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
        var otsu = new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
        otsu.Apply(work);
        Assert.InRange(otsu.CalculatedThreshold, 0, 255);
        foreach (int idx in sampleIndices)
            Assert.True(work.Pixels[idx].R is 0 or 255);

        // 4. ASCII & Resize
        string ascii = new AsciiConverter { AsciiMode = AsciiMode.Simple, BlockSize = Math.Max(8, width / 128) }.Convert(source);
        Assert.NotEmpty(ascii);

        var resized = ImageResizer.Resize(source, 320);
        Assert.Equal(320, resized.Width);

        // 5. BoxBlur & Fisheye on all extreme sizes (freeing work copy proactively after)
        Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
        new BoxBlurFilter().Apply(work);

        Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
        new FisheyeFilter(1.0).Apply(work);

        work = null!;
        source = null!;
        GC.Collect();
    }
}

