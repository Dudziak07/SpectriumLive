using SpectriumLive.Application.Helpers;
using SpectriumLive.Core.Entities;

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
    protected override bool UseAbsoluteValue => true;

    public override void Apply(ImageBuffer imageBuffer) =>
        ConvolutionSimdHelper.ApplyEdgeDetection3x3(imageBuffer);
}
