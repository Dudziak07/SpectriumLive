namespace SpectriumLive.Application.Filters.Convolution;
public class GaussianBlurFilter : ConvolutionFilterBase
{
    public override string Name => "Gaussian Blur";
    protected override double[,] Kernel { get; } =
    {
        { 1, 4, 7, 4, 1 },
        { 4, 16, 26, 16, 4 },
        { 7, 26, 41, 26, 7 },
        { 4, 16, 26, 16, 4 },
        { 1, 4, 7, 4, 1 }
    };
    protected override double Factor => 1.0 / 273.0;
}
