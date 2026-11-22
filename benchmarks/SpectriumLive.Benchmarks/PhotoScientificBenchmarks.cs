using BenchmarkDotNet.Attributes;

namespace SpectriumLive.Benchmarks;

[BenchmarkCategory("PhotoScientific")]
public class PhotoScientificBenchmarks : FilterBenchmarksBase
{
    [ParamsAllValues]
    public PhotoScientificResolution Resolution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (w, h) = BenchmarkImageData.GetDimensions(Resolution);
        InitializeBuffers(w, h);
    }
}

