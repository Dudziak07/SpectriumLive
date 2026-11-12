using System.Runtime.InteropServices;

namespace SpectriumLive.Core.Entities;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly record struct PixelData(byte R, byte G, byte B, byte A);
