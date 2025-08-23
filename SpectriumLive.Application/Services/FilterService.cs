using SpectriumLive.Application.Filters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Services;

public class FilterService
{
    public static IEnumerable<IFilter> GetAvailableFilters()
    {
        yield return new BinaryFilter();
        yield return new GrayscaleFilter();
        yield return new NegativeFilter();
        yield return new SepiaFilter();

        yield return new SwapChannelsFilter("Swap Channels (Red-Blue)", SwapMode.RB);
        yield return new SwapChannelsFilter("Swap Channels (Red-Green)", SwapMode.RG);
        yield return new SwapChannelsFilter("Swap Channels (Green-Blue)", SwapMode.GB);
    }
}
