using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Helpers;

public class ColorCalculator
{
    public static byte ToGrayscale(PixelData pixel)
    {
        var gray = 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;

        return (byte)gray;
    }
}
