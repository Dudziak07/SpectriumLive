namespace SpectriumLive.Application.Filters.Convolution;
public class EdgeDetectionFilter : ConvolutionFilterBase
{
    public override string Name => "Edge Detection";
    protected override double[,] Kernel { get; } =
    {
        { -1, -1, -1 },
        { -1, 8, -1 },
        { -1, -1, -1 }
    };
    protected override bool UseAbsoulteValue => true;
}
