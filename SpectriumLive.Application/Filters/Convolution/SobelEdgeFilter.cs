using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters.Convolution;

public class SobelEdgeFilter : IFilter
{
    public string Name => "Sobel Edge";
    public ImageProcessorGroup Group => ImageProcessorGroup.Convolution;
    public bool HasConfigurableProperties => false;

    public void Apply(ImageBuffer imageBuffer) =>
        ConvolutionSimdHelper.ApplySobel3x3(imageBuffer);
}
