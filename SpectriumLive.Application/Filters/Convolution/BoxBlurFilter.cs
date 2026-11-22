using SpectriumLive.Application.Helpers;
using SpectriumLive.Core.Entities;

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

    public override void Apply(ImageBuffer imageBuffer) =>
        ConvolutionSimdHelper.ApplyBoxBlur3x3(imageBuffer);
}
