using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

internal class NegativeFilter : IFilter
{
    public string Name => "Negative";

    public void Apply(ImageBuffer imageBuffer)
    {
        for ( int i = 0; i < imageBuffer.Pixels.Length; i++ )
        {
            var pixel = imageBuffer.Pixels[i];

            byte newR = (byte)( 255 - pixel.R );
            byte newG = (byte)( 255 - pixel.G );
            byte newB = (byte)( 255 - pixel.B );

            imageBuffer.Pixels[i] = new(newR, newG, newB, pixel.A);
        }
    }
}
