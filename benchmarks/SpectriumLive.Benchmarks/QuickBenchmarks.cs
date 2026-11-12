using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("Quick")]
public class QuickBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public QuickResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h);
    }
}

