using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.Tests.TestHelpers;
using SpectriumLive.Application.TextConverters;
using Xunit;

namespace SpectriumLive.Application.Tests.Quality;

[Trait("Category", "RealAssets")]
public class RealWorldImageTests
{
    [Fact]
    public void KodakSuite_All24Images_PassQualityOraclesAndConverters()
    {
        var files = RealAssetLoader.GetKodakFiles().ToList();
        Assert.Equal(24, files.Count);

        foreach (string file in files)
        {
            var source = RealAssetLoader.LoadAsImageBuffer(file);
            Assert.True(
                (source.Width == 768 && source.Height == 512) || (source.Width == 512 && source.Height == 768),
                $"Unexpected Kodak dimensions {source.Width}x{source.Height} for {Path.GetFileName(file)}");

            // Negative exact
            var neg = TestImageFactory.Clone(source);
            new NegativeFilter().Apply(neg);
            Assert.Equal(0, TestImageFactory.Compare(ReferenceFilterOracles.ReferenceNegative(source), neg).MaxChannelDelta);

            // Grayscale
            var gray = TestImageFactory.Clone(source);
            new GrayscaleFilter().Apply(gray);
            var grayMetrics = TestImageFactory.Compare(ReferenceFilterOracles.ReferenceGrayscale(source), gray);
            Assert.True(grayMetrics.MaxChannelDelta <= 1, $"{Path.GetFileName(file)} Grayscale delta: {grayMetrics.MaxChannelDelta}");

            // Sepia
            var sepia = TestImageFactory.Clone(source);
            new SepiaFilter().Apply(sepia);
            var sepiaMetrics = TestImageFactory.Compare(ReferenceFilterOracles.ReferenceSepia(source), sepia);
            Assert.True(sepiaMetrics.MaxChannelDelta <= 1, $"{Path.GetFileName(file)} Sepia delta: {sepiaMetrics.MaxChannelDelta}");

            // Gaussian 5x5
            var gauss = TestImageFactory.Clone(source);
            new GaussianBlurFilter { KernelSize = 5, Sigma = 1.4 }.Apply(gauss);
            var gaussOracle = ReferenceFilterOracles.ReferenceGaussian2D(source, 5, 1.4);
            var gaussMetrics = TestImageFactory.Compare(gaussOracle, gauss, ignoreBorder: true, borderOffset: 2);
            Assert.True(gaussMetrics.MaxChannelDelta <= 1, $"{Path.GetFileName(file)} Gaussian delta: {gaussMetrics.MaxChannelDelta}");
            Assert.True(gaussMetrics.PsnrDb >= 48.0 || double.IsPositiveInfinity(gaussMetrics.PsnrDb));

            // Otsu Binarization
            var otsuImg = TestImageFactory.Clone(source);
            var otsu = new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
            otsu.Apply(otsuImg);
            Assert.InRange(otsu.CalculatedThreshold, 1, 254);

            // ASCII
            var ascii = new AsciiConverter { AsciiMode = AsciiMode.Full, BlockSize = 4 }.Convert(source);
            Assert.NotEmpty(ascii);
        }
    }

    [Fact]
    public void HighResPublicDomainPhotos_All3Images_PassAllFiltersWithoutError()
    {
        var files = RealAssetLoader.GetHighResFiles().ToList();
        Assert.Equal(3, files.Count);

        foreach (string file in files)
        {
            var source = RealAssetLoader.LoadAsImageBuffer(file);
            Assert.True(source.Width >= 3840, $"Expected high-res image >= 3840px wide, got {source.Width} for {Path.GetFileName(file)}");

            var work = TestImageFactory.Clone(source);
            new GrayscaleFilter().Apply(work);
            var grayMetrics = TestImageFactory.Compare(ReferenceFilterOracles.ReferenceGrayscale(source), work);
            Assert.True(grayMetrics.MaxChannelDelta <= 1);

            Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
            new GaussianBlurFilter { KernelSize = 5, Sigma = 1.4 }.Apply(work);
            var gaussMetrics = TestImageFactory.Compare(
                ReferenceFilterOracles.ReferenceGaussian2D(source, 5, 1.4),
                work,
                ignoreBorder: true,
                borderOffset: 2);
            Assert.True(gaussMetrics.MaxChannelDelta <= 1);

            Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
            var otsu = new BinaryFilter { SelectedMethod = BinarizationMethod.Otsu };
            otsu.Apply(work);
            Assert.InRange(otsu.CalculatedThreshold, 1, 254);

            Array.Copy(source.Pixels, work.Pixels, source.Pixels.Length);
            new FisheyeFilter(1.0).Apply(work);

            string ascii = new AsciiConverter { AsciiMode = AsciiMode.Simple, BlockSize = 16 }.Convert(source);
            Assert.NotEmpty(ascii);

            var resized = ImageResizer.Resize(source, 640);
            Assert.Equal(640, resized.Width);

            work = null!;
            source = null!;
            GC.Collect();
        }
    }
}
