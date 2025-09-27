using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Services;
public class ProcessorService
{
    public IEnumerable<IProcessor> GetAvailableProcessors()
    {
        // Color
        yield return new BinaryFilter();
        yield return new GrayscaleFilter();
        yield return new NegativeFilter();
        yield return new SepiaFilter();
        yield return new SwapChannelsFilter("Swap Channels (Red-Blue)", SwapMode.RB);
        yield return new SwapChannelsFilter("Swap Channels (Red-Green)", SwapMode.RG);
        yield return new SwapChannelsFilter("Swap Channels (Green-Blue)", SwapMode.GB);
        yield return new ChannelFilter("Red Channel", ChannelSelector.R);
        yield return new ChannelFilter("Green Channel", ChannelSelector.G);
        yield return new ChannelFilter("Blue Channel", ChannelSelector.B);

        // Geometric
        yield return new FisheyeFilter(1.0); // Fisheye
        yield return new FisheyeFilter(-1.0); // Defisheye

        // Convolution
        yield return new BoxBlurFilter();
        yield return new GaussianBlurFilter();
        yield return new EdgeDetectionFilter();

        // ASCII
        yield return new AsciiConverter("ASCII (Binary)", AsciiCharacterRamps.Binary);
        yield return new AsciiConverter("ASCII (Simple)", AsciiCharacterRamps.Simple);
        yield return new AsciiConverter("ASCII (Full)", AsciiCharacterRamps.Full);
    }
}
