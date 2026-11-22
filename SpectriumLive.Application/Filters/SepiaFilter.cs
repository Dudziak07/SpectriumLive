using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;

public class SepiaFilter : IFilter
{
    public string Name => "Sepia";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => false;

    public void Apply(ImageBuffer imageBuffer)
    {
        PixelSimdHelper.ForEachSlice(imageBuffer.Pixels, static slice =>
            PixelSimdHelper.ConvertToSepia(slice));
    }
}
