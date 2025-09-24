using SpectriumLive.Application.Filters;
using SpectriumLive.Application.Filters.Convolution;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Services;

public class FilterService
{
    public IEnumerable<IFilter> GetAvailableFilters()
    {
        yield return new BinaryFilter();
        yield return new GrayscaleFilter();
        yield return new NegativeFilter();
        yield return new SepiaFilter();
        yield return new FisheyeFilter(1.0);
        yield return new FisheyeFilter(-1.0);

        yield return new SwapChannelsFilter("Swap Channels (Red-Blue)", SwapMode.RB);
        yield return new SwapChannelsFilter("Swap Channels (Red-Green)", SwapMode.RG);
        yield return new SwapChannelsFilter("Swap Channels (Green-Blue)", SwapMode.GB);

        yield return new ChannelFilter("Red Channel", ChannelSelector.R);
        yield return new ChannelFilter("Green Channel", ChannelSelector.G);
        yield return new ChannelFilter("Blue Channel", ChannelSelector.B);

        // Convolution
        yield return new BoxBlurFilter();
        yield return new GaussianBlurFilter();
        yield return new EdgeDetectionFilter();
    }
}
