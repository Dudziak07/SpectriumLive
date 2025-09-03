using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

public class GrayscaleFilter : IFilter
{
    public string Name => "Grayscale";

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            byte grayByte = ColorCalculator.ToGrayscale(pixel);

            imageBuffer.Pixels[i] = new(grayByte, grayByte, grayByte, pixel.A);
        });
    }
}
