using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;
public class SwapChannelsFilter : IFilter
{
    public string Name => "Swap Channels (R-B)";

    public void Apply(ImageBuffer imageBuffer)
    {
        for ( var i = 0; i < imageBuffer.Pixels.Length; i++ )
        {
            var pixel = imageBuffer.Pixels[i];

            imageBuffer.Pixels[i] = new PixelData(pixel.B, pixel.G, pixel.R, pixel.A);
        }
    }
}
