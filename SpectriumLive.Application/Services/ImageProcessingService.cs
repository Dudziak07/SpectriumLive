using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Services;
public class ImageProcessingService
{
    public IEnumerable<IImageProcessor> GetAvailableProcessors()
    {
        // ASCII
        yield return new AsciiConverter();

        // Color
        yield return new BinaryFilter();
        yield return new BrightnessContrastFilter();
        yield return new GrayscaleFilter();
        yield return new NegativeFilter();
        yield return new SepiaFilter();
        yield return new SwapChannelsFilter();
        yield return new ChannelFilter();

        // Convolution
        yield return new BoxBlurFilter();
        yield return new GaussianBlurFilter();
        yield return new EdgeDetectionFilter();
        yield return new SharpenFilter();
        yield return new SobelEdgeFilter();

        // Distortion
        yield return new FisheyeFilter(1.0); // Fisheye
    }
}
