namespace SpectriumLive.Core.Entities;

public class PixelData(byte r, byte g, byte b, byte a)
{
    public byte R { get; set; } = r;
    public byte G { get; set; } = g;
    public byte B { get; set; } = b;
    public byte A { get; set; } = a;
}
