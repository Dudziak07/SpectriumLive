using SpectriumLive.Application.Helpers;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters.Convolution;

public class SharpenFilter : ConvolutionFilterBase
{
    public override string Name => "Sharpen";
    protected override double[,] Kernel { get; } =
    {
        { 0, -1, 0 },
        { -1, 5, -1 },
        { 0, -1, 0 }
    };

    public override void Apply(ImageBuffer imageBuffer) =>
        ConvolutionSimdHelper.ApplySharpen3x3(imageBuffer);
}
