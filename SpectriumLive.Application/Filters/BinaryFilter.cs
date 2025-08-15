using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

public class BinaryFilter : IFilter
{
    public string Name => "Binary";
    private const byte Threshold = 128;

    public void Apply(ImageBuffer imageBuffer)
    {
        for ( int i = 0; i < imageBuffer.Pixels.Length; i++ )
        {
            var pixel = imageBuffer.Pixels[i];

            var gray = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
            byte finalValue = gray < Threshold
                ? (byte)0
                : (byte)255;

            imageBuffer.Pixels[i] = new(finalValue, finalValue, finalValue, pixel.A);
        }
    }
}
