using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;

public enum ChannelSelector
{
    R,
    G,
    B
}

public class ChannelFilter(string name, ChannelSelector channelSelector) : IFilter
{
    public string Name { get; } = name;
    public string Group => "Color";
    private readonly ChannelSelector channelSelector = channelSelector;

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            imageBuffer.Pixels[i] = channelSelector switch
            {
                ChannelSelector.R => new PixelData(pixel.R, 0, 0, pixel.A),
                ChannelSelector.G => new PixelData(0, pixel.G, 0, pixel.A),
                ChannelSelector.B => new PixelData(0, 0, pixel.B, pixel.A),
                _ => pixel
            };
        });
    }
}
