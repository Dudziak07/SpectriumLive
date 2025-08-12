namespace SpectriumLive.Core;

public class ImageBuffer(int width, int height, PixelData[] pixels)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public PixelData[] Pixels { get; } = pixels;
}
