using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;

public class NegativeFilter : IFilter
{
    public string Name => "Negative";

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            byte newR = (byte)( 255 - pixel.R );
            byte newG = (byte)( 255 - pixel.G );
            byte newB = (byte)( 255 - pixel.B );

            imageBuffer.Pixels[i] = new(newR, newG, newB, pixel.A);
        });
    }
}
