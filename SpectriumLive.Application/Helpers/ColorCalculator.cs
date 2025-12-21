using SpectriumLive.Core.Entities;

namespace SpectriumLive.Application.Helpers;

public static class ColorCalculator
{
    public static byte ToGrayscale(PixelData pixel) => (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
}
