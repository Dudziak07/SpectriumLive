using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("Standard")]
public class StandardFormatsBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public StandardFormatResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h);
    }
}

