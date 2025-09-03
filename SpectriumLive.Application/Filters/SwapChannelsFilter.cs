using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

public enum SwapMode
{
    RB,
    RG,
    GB
}

public class SwapChannelsFilter(string name, SwapMode swapMode) : IFilter
{
    public string Name { get; } = name;
    private readonly SwapMode swapMode = swapMode;

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            imageBuffer.Pixels[i] = swapMode switch
            {
                SwapMode.RB => new PixelData(pixel.B, pixel.G, pixel.R, pixel.A),
                SwapMode.RG => new PixelData(pixel.G, pixel.R, pixel.B, pixel.A),
                SwapMode.GB => new PixelData(pixel.R, pixel.B, pixel.G, pixel.A),
                _ => pixel
            };
        });
    }
}
