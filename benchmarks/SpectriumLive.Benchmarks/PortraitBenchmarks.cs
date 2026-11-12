using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("Portrait")]
public class PortraitBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public PortraitFormatResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h);
    }
}

