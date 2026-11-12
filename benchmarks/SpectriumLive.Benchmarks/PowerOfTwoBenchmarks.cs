using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("PowerOf2")]
public class PowerOfTwoBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public PowerOfTwoResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h);
    }
}

