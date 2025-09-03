using SpectriumLive.Core;

namespace SpectriumLive.Application.Filters;

public class SepiaFilter : IFilter
{
    public string Name => "Sepia";

    public void Apply(ImageBuffer imageBuffer)
    {
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            double newR = 0.393 * pixel.R + 0.769 * pixel.G + 0.189 * pixel.B;
            double newG = 0.349 * pixel.R + 0.686 * pixel.G + 0.168 * pixel.B;
            double newB = 0.272 * pixel.R + 0.534 * pixel.G + 0.131 * pixel.B;

            byte finalR = (byte)Math.Min(255, newR);
            byte finalG = (byte)Math.Min(255, newG);
            byte finalB = (byte)Math.Min(255, newB);

            imageBuffer.Pixels[i] = new(finalR, finalG, finalB, pixel.A);
        });
    }
}
