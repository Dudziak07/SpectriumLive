using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

public class GrayscaleFilter : IFilter
{
    public string Name => "Grayscale";

    public void Apply(ImageBuffer imageBuffer)
    {
        for ( int i = 0; i < imageBuffer.Pixels.Length; i++ )
        {
            var pixel = imageBuffer.Pixels[i];

            var gray = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
            byte grayByte = (byte)gray;

            imageBuffer.Pixels[i] = new(grayByte, grayByte, grayByte, pixel.A);
        }
    }
}
