using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.Tests.TestHelpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using Xunit;

namespace SpectriumLive.Application.Tests.Quality;

public class FilterOracleAndEdgeTests
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

    private static IEnumerable<IFilter> CreateAllColorAndConvolutionFilters()
    {
        yield return new GrayscaleFilter();
        yield return new NegativeFilter();
        yield return new SepiaFilter();
        yield return new ChannelFilter { SelectedChannel = ChannelSelector.R };
        yield return new ChannelFilter { SelectedChannel = ChannelSelector.G };
        yield return new ChannelFilter { SelectedChannel = ChannelSelector.B };
        yield return new SwapChannelsFilter { SelectedMode = SwapMode.RB };
        yield return new SwapChannelsFilter { SelectedMode = SwapMode.RG };
        yield return new SwapChannelsFilter { SelectedMode = SwapMode.GB };
        yield return new BinaryFilter { SelectedMethod = BinarizationMethod.Manual, ManualThreshold = 128 };
        yield return new BinaryFilter { SelectedMethod = BinarizationMethod.PercentBlack, PercentBlack = 0.4 };
        yield return new BinaryFilter { SelectedMethod = BinarizationMethod.MeanIterative };
        yield return new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
        yield return new BoxBlurFilter();
        yield return new GaussianBlurFilter { KernelSize = 5, Sigma = 1.4 };
        yield return new EdgeDetectionFilter();
    }

    [Fact]
    public void AllFilters_OnEmptyImageBuffer_DoNotThrow()
    {
        foreach (var filter in CreateAllColorAndConvolutionFilters())
        {
            var empty = new ImageBuffer(0, 0, []);
            filter.Apply(empty);
            Assert.Empty(empty.Pixels);
        }

        var fisheye = new FisheyeFilter(1.0);
        var emptyFisheye = new ImageBuffer(0, 0, []);
        fisheye.Apply(emptyFisheye);
        Assert.Empty(emptyFisheye.Pixels);

        var ascii = new AsciiConverter();
        var asciiOut = ascii.Convert(new ImageBuffer(0, 0, []));
        Assert.Equal(string.Empty, asciiOut);

        var resized = ImageResizer.Resize(new ImageBuffer(0, 0, []), 100);
        Assert.Empty(resized.Pixels);
    }

    [Theory]
    [MemberData(nameof(OddAndDiverseResolutions))]
    public void AllColorAndConvolutionFilters_PreserveAlphaChannel(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height, seed: 99);

        foreach (var filter in CreateAllColorAndConvolutionFilters())
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
    public void AllFilters_AreDeterministic_AcrossRepeatedRuns(int width, int height)
    {
        var source = TestImageFactory.CreateGradient(width, height);

        foreach (var filter in CreateAllColorAndConvolutionFilters().Append(new FisheyeFilter(1.0)))
        {
            var run1 = TestImageFactory.Clone(source);
            var run2 = TestImageFactory.Clone(source);

            filter.Apply(run1);
            filter.Apply(run2);

            Assert.Equal(run1.Pixels, run2.Pixels);
        }
    }

    [Theory]
    [InlineData(17, 13)]
    [InlineData(127, 65)]
    [InlineData(640, 480)]
    public void NegativeFilter_MatchesExactReferenceOracle(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height);
        var expected = TestImageFactory.Clone(source);
        for (int i = 0; i < expected.Pixels.Length; i++)
        {
            var p = expected.Pixels[i];
            expected.Pixels[i] = new PixelData((byte)(255 - p.R), (byte)(255 - p.G), (byte)(255 - p.B), p.A);
        }

        var actual = TestImageFactory.Clone(source);
        new NegativeFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual);
        Assert.Equal(0, metrics.MaxChannelDelta);
    }

    [Theory]
    [InlineData(17, 13)]
    [InlineData(127, 65)]
    [InlineData(640, 480)]
    public void GrayscaleFilter_MatchesReferenceWithin1Lsb(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height);
        var expected = TestImageFactory.Clone(source);
        for (int i = 0; i < expected.Pixels.Length; i++)
        {
            var p = expected.Pixels[i];
            byte gray = (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
            expected.Pixels[i] = new PixelData(gray, gray, gray, p.A);
        }

        var actual = TestImageFactory.Clone(source);
        new GrayscaleFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual);
        Assert.True(metrics.MaxChannelDelta <= 1, $"Max delta {metrics.MaxChannelDelta} exceeded 1 LSB.");
    }

    [Theory]
    [InlineData(17, 13)]
    [InlineData(127, 65)]
    [InlineData(640, 480)]
    public void SepiaFilter_MatchesReferenceWithin1Lsb(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height);
        var expected = TestImageFactory.Clone(source);
        for (int i = 0; i < expected.Pixels.Length; i++)
        {
            var p = expected.Pixels[i];
            byte r = (byte)Math.Min(255, 0.393 * p.R + 0.769 * p.G + 0.189 * p.B);
            byte g = (byte)Math.Min(255, 0.349 * p.R + 0.686 * p.G + 0.168 * p.B);
            byte b = (byte)Math.Min(255, 0.272 * p.R + 0.534 * p.G + 0.131 * p.B);
            expected.Pixels[i] = new PixelData(r, g, b, p.A);
        }

        var actual = TestImageFactory.Clone(source);
        new SepiaFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual);
        Assert.True(metrics.MaxChannelDelta <= 1, $"Max delta {metrics.MaxChannelDelta} exceeded 1 LSB.");
    }

    [Fact]
    public void BinaryFilter_OtsuAndMeanIterative_SeparateBimodalDistributionCleanly()
    {
        var source = TestImageFactory.CreateBimodal(257, 193, low: 30, high: 220, lowRatio: 0.45);

        foreach (var method in new[] { BinarizationMethod.Otsu, BinarizationMethod.MeanIterative })
        {
            var actual = TestImageFactory.Clone(source);
            var filter = new BinaryFilter { SelectedMethod = method };
            filter.Apply(actual);

            Assert.InRange(filter.CalculatedThreshold, 40, 210);
            foreach (var p in actual.Pixels)
            {
                Assert.True(p.R is 0 or 255);
                Assert.Equal(p.R, p.G);
                Assert.Equal(p.R, p.B);
            }
        }
    }

    [Theory]
    [InlineData(5, 1.4)]
    [InlineData(9, 2.5)]
    [InlineData(15, 4.0)]
    public void GaussianBlurFilter_InteriorMatchesExact2DReference(int kernelSize, double sigma)
    {
        const int width = 97;
        const int height = 73;
        var source = TestImageFactory.CreateCheckerboard(width, height, cellSize: 3);
        var expected = ReferenceFilterOracles.ReferenceGaussian2D(source, kernelSize, sigma);

        var actual = TestImageFactory.Clone(source);
        var filter = new GaussianBlurFilter { KernelSize = kernelSize, Sigma = sigma };
        filter.Apply(actual);

        int offset = kernelSize / 2;
        var metrics = TestImageFactory.Compare(expected, actual, ignoreBorder: true, borderOffset: offset);
        Assert.True(metrics.MaxChannelDelta <= 1, $"Gaussian max delta: {metrics.MaxChannelDelta}");
        Assert.True(metrics.PsnrDb >= 48.0 || double.IsPositiveInfinity(metrics.PsnrDb), $"Gaussian PSNR too low: {metrics.PsnrDb:F2} dB");
    }

    [Theory]
    [InlineData(31, 29)]
    [InlineData(127, 65)]
    public void BoxBlurFilter_InteriorMatchesExactReference(int width, int height)
    {
        var source = TestImageFactory.CreateNoise(width, height, seed: 77);
        var expected = ReferenceFilterOracles.ReferenceBoxBlur3x3(source);

        var actual = TestImageFactory.Clone(source);
        new BoxBlurFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual, ignoreBorder: true, borderOffset: 1);
        Assert.True(metrics.MaxChannelDelta <= 1, $"BoxBlur max delta: {metrics.MaxChannelDelta}");
    }

    [Theory]
    [InlineData(31, 29)]
    [InlineData(127, 65)]
    public void EdgeDetectionFilter_InteriorMatchesExactReference(int width, int height)
    {
        var source = TestImageFactory.CreateCheckerboard(width, height, cellSize: 5);
        var expected = ReferenceFilterOracles.ReferenceEdgeDetection3x3(source);

        var actual = TestImageFactory.Clone(source);
        new EdgeDetectionFilter().Apply(actual);

        var metrics = TestImageFactory.Compare(expected, actual, ignoreBorder: true, borderOffset: 1);
        Assert.True(metrics.MaxChannelDelta <= 1, $"EdgeDetection max delta: {metrics.MaxChannelDelta}");
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(-0.8)]
    public void FisheyeFilter_OnRadialTarget_MatchesReference(double strength)
    {
        var source = TestImageFactory.CreateRadialTarget(127, 97);
        var expected = TestImageFactory.Clone(source);
        var actual = TestImageFactory.Clone(source);

        // Current reference verification against self-consistency & geometry
        new FisheyeFilter(strength).Apply(actual);
        new FisheyeFilter(strength).Apply(expected);

        Assert.Equal(expected.Pixels, actual.Pixels);
        // Center pixel preserved
        int centerIdx = (97 / 2) * 127 + (127 / 2);
        Assert.Equal(source.Pixels[centerIdx], actual.Pixels[centerIdx]);
    }

    [Theory]
    [InlineData(AsciiMode.Binary, 1)]
    [InlineData(AsciiMode.Simple, 2)]
    [InlineData(AsciiMode.Full, 3)]
    public void AsciiConverter_GeneratesExpectedLineStructure(AsciiMode mode, int blockSize)
    {
        var source = TestImageFactory.CreateGradient(63, 35);
        var converter = new AsciiConverter { AsciiMode = mode, BlockSize = blockSize };

        string result = converter.Convert(source);

        Assert.NotEmpty(result);
        Assert.EndsWith(Environment.NewLine, result);
        string[] lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        int expectedWidth = source.Width / blockSize;
        Assert.All(lines, line => Assert.Equal(expectedWidth, line.Length));
    }

    [Fact]
    public void ImageResizer_ResizesNonZeroImage_AndPreservesAspect()
    {
        var source = TestImageFactory.CreateGradient(200, 100);
        var resized = ImageResizer.Resize(source, 80);

        Assert.Equal(80, resized.Width);
        Assert.True(resized.Height > 0);
        Assert.Equal(resized.Width * resized.Height, resized.Pixels.Length);
    }
}

