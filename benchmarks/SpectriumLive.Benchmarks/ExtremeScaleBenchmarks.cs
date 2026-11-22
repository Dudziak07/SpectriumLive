using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("Extreme")]
public class ExtremeScaleBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public ExtremeResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h, keepSourceCopy: false);
    }
}

