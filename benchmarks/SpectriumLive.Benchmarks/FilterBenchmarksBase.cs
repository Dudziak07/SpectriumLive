using BenchmarkDotNet.Attributes;
using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Benchmarks;

[MemoryDiagnoser]
public abstract class FilterBenchmarksBase
{
    private ImageBuffer _buffer = null!;
    private PixelData[]? _sourcePixels;

    private readonly GrayscaleFilter _grayscale = new();
    private readonly NegativeFilter _negative = new();
    private readonly SepiaFilter _sepia = new();
    private readonly ChannelFilter _channelR = new() { SelectedChannel = ChannelSelector.R };
    private readonly SwapChannelsFilter _swapRb = new() { SelectedMode = SwapMode.RB };
    private readonly BinaryFilter _binaryManual = new() { SelectedMethod = BinarizationMethod.Manual, ManualThreshold = 128 };
    private readonly BinaryFilter _binaryOtsu = new() { SelectedMethod = BinarizationMethod.Otsu };
    private readonly BoxBlurFilter _boxBlur = new();
    private readonly GaussianBlurFilter _gaussian5 = new() { KernelSize = 5, Sigma = 1.4 };
    private readonly GaussianBlurFilter _gaussian15 = new() { KernelSize = 15, Sigma = 4.0 };
    private readonly EdgeDetectionFilter _edgeDetection = new();
    private readonly FisheyeFilter _fisheye = new(1.0);
    private readonly AsciiConverter _asciiSimple = new() { AsciiMode = AsciiMode.Simple, BlockSize = 4 };
    private readonly AsciiConverter _asciiFull = new() { AsciiMode = AsciiMode.Full, BlockSize = 4 };

    protected void InitializeBuffers(int width, int height, bool keepSourceCopy = true)
    {
        _buffer = BenchmarkImageData.CreateRealisticBuffer(width, height);
        _sourcePixels = keepSourceCopy ? (PixelData[])_buffer.Pixels.Clone() : null;
    }

    [GlobalCleanup]
    public void CleanupBuffers()
    {
        _buffer = null!;
        _sourcePixels = null;
    }

    [IterationSetup(Target = nameof(BinaryOtsu))]
    public void ResetPixelsForOtsu()
    {
        if (_sourcePixels is not null)
        {
            Array.Copy(_sourcePixels, _buffer.Pixels, _sourcePixels.Length);
        }
    }

    [Benchmark]
    public void Grayscale() => _grayscale.Apply(_buffer);

    [Benchmark]
    public void Negative() => _negative.Apply(_buffer);

    [Benchmark]
    public void Sepia() => _sepia.Apply(_buffer);

    [Benchmark]
    public void ChannelR() => _channelR.Apply(_buffer);

    [Benchmark]
    public void SwapRB() => _swapRb.Apply(_buffer);

    [Benchmark]
    public void BinaryManual() => _binaryManual.Apply(_buffer);

    [Benchmark]
    public void BinaryOtsu() => _binaryOtsu.Apply(_buffer);

    [Benchmark]
    public void BoxBlur3x3() => _boxBlur.Apply(_buffer);

    [Benchmark]
    public void GaussianBlur5x5() => _gaussian5.Apply(_buffer);

    [Benchmark]
    public void GaussianBlur15x15() => _gaussian15.Apply(_buffer);

    [Benchmark]
    public void EdgeDetection3x3() => _edgeDetection.Apply(_buffer);

    [Benchmark]
    public void Fisheye() => _fisheye.Apply(_buffer);

    [Benchmark]
    public ImageBuffer ResizeTo320() => ImageResizer.Resize(_buffer, 320);

    [Benchmark]
    public string AsciiSimple() => _asciiSimple.Convert(_buffer);

    [Benchmark]
    public string AsciiFull() => _asciiFull.Convert(_buffer);
}

