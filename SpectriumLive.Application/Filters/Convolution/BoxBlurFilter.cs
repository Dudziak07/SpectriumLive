namespace SpectriumLive.Application.Filters.Convolution;
public class BoxBlurFilter : ConvolutionFilterBase
{
    public override string Name => "Box Blur";
    protected override double[,] Kernel { get; } =
    {
        { 1, 1, 1 },
        { 1, 1, 1 },
        { 1, 1, 1 }
    };
    protected override double Factor => 1.0 / 9.0;
}
