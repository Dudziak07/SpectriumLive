using SpectriumLive.Application.Helpers;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Filters;

public class BinaryFilter : IFilter
{
    public string Name => "Binary";
    public string Group => "Color";
    private const byte Threshold = 128;

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            var gray = ColorCalculator.ToGrayscale(pixel);
            byte finalValue = gray < Threshold
                ? (byte)0
                : (byte)255;

            imageBuffer.Pixels[i] = new(finalValue, finalValue, finalValue, pixel.A);
        });
    }
}
