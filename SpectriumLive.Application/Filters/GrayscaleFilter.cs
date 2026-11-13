using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;

public class GrayscaleFilter : IFilter
{
    public string Name => "Grayscale";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => false;

    public void Apply(ImageBuffer imageBuffer)
    {
        PixelSimdHelper.ForEachSlice(imageBuffer.Pixels, static slice =>
            PixelSimdHelper.ConvertToGrayscale(slice));
    }
}
