using SpectriumLive.Core.Entities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SpectriumLive.Application.Helpers;

internal delegate void PixelSliceAction(Span<PixelData> slice);

internal delegate void PixelSliceAction<in TState>(Span<PixelData> slice, TState state);

internal static class PixelSimdHelper
{
    // Below 65,536 pixels (e.g. 256x256), single-threaded SIMD finishes faster than Parallel.For scheduling overhead.
    internal const int ParallelPixelThreshold = 65_536;
    private const int ChunkAlignment = 16; // 64 bytes = 1 Vector512<uint> / cache line

    public static void ForEachSlice(PixelData[] pixels, PixelSliceAction action)
    {
        int length = pixels.Length;
        if (length == 0)
            return;

        if (length < ParallelPixelThreshold || Environment.ProcessorCount <= 1)
        {
            action(pixels.AsSpan());
            return;
        }

        int partitions = Math.Min(Environment.ProcessorCount, length / (ParallelPixelThreshold / 2));
        if (partitions <= 1)
        {
            action(pixels.AsSpan());
            return;
        }

        int chunkSize = ((length + partitions - 1) / partitions + (ChunkAlignment - 1)) & ~(ChunkAlignment - 1);
        int chunkCount = (length + chunkSize - 1) / chunkSize;

        Parallel.For(0, chunkCount, chunkIndex =>
        {
            int start = chunkIndex * chunkSize;
            int count = Math.Min(chunkSize, length - start);
            action(pixels.AsSpan(start, count));
        });
    }

    public static void ForEachSlice<TState>(PixelData[] pixels, TState state, PixelSliceAction<TState> action)
    {
        int length = pixels.Length;
        if (length == 0)
            return;

        if (length < ParallelPixelThreshold || Environment.ProcessorCount <= 1)
        {
            action(pixels.AsSpan(), state);
            return;
        }

        int partitions = Math.Min(Environment.ProcessorCount, length / (ParallelPixelThreshold / 2));
        if (partitions <= 1)
        {
            action(pixels.AsSpan(), state);
            return;
        }

        int chunkSize = ((length + partitions - 1) / partitions + (ChunkAlignment - 1)) & ~(ChunkAlignment - 1);
        int chunkCount = (length + chunkSize - 1) / chunkSize;

        Parallel.For(0, chunkCount, chunkIndex =>
        {
            int start = chunkIndex * chunkSize;
            int count = Math.Min(chunkSize, length - start);
            action(pixels.AsSpan(start, count), state);
        });
    }

    public static void ApplyXorMask(Span<PixelData> slice, uint leMask)
    {
        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                slice[k] = new PixelData((byte)(255 - p.R), (byte)(255 - p.G), (byte)(255 - p.B), p.A);
            }
            return;
        }

        Span<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
        ref uint ptr = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Vector512.IsHardwareAccelerated && len >= Vector512<uint>.Count)
        {
            var mask512 = Vector512.Create(leMask);
            int end = len - Vector512<uint>.Count;
            for (; i <= end; i += Vector512<uint>.Count)
            {
                Vector512<uint> v = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                (v ^ mask512).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector256.IsHardwareAccelerated && i <= len - Vector256<uint>.Count)
        {
            var mask256 = Vector256.Create(leMask);
            int end = len - Vector256<uint>.Count;
            for (; i <= end; i += Vector256<uint>.Count)
            {
                Vector256<uint> v = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                (v ^ mask256).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - Vector128<uint>.Count)
        {
            var mask128 = Vector128.Create(leMask);
            int end = len - Vector128<uint>.Count;
            for (; i <= end; i += Vector128<uint>.Count)
            {
                Vector128<uint> v = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                (v ^ mask128).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < len; i++)
        {
            ref uint p = ref Unsafe.Add(ref ptr, i);
            p ^= leMask;
        }
    }

    public static void ApplyAndMask(Span<PixelData> slice, uint leMask)
    {
        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                slice[k] = new PixelData(
                    (leMask & 0xFFu) != 0 ? p.R : (byte)0,
                    (leMask & 0xFF00u) != 0 ? p.G : (byte)0,
                    (leMask & 0xFF0000u) != 0 ? p.B : (byte)0,
                    p.A);
            }
            return;
        }

        Span<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
        ref uint ptr = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Vector512.IsHardwareAccelerated && len >= Vector512<uint>.Count)
        {
            var mask512 = Vector512.Create(leMask);
            int end = len - Vector512<uint>.Count;
            for (; i <= end; i += Vector512<uint>.Count)
            {
                Vector512<uint> v = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                (v & mask512).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector256.IsHardwareAccelerated && i <= len - Vector256<uint>.Count)
        {
            var mask256 = Vector256.Create(leMask);
            int end = len - Vector256<uint>.Count;
            for (; i <= end; i += Vector256<uint>.Count)
            {
                Vector256<uint> v = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                (v & mask256).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - Vector128<uint>.Count)
        {
            var mask128 = Vector128.Create(leMask);
            int end = len - Vector128<uint>.Count;
            for (; i <= end; i += Vector128<uint>.Count)
            {
                Vector128<uint> v = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                (v & mask128).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < len; i++)
        {
            ref uint p = ref Unsafe.Add(ref ptr, i);
            p &= leMask;
        }
    }

    public static void ShuffleChannels(Span<PixelData> slice, uint lePixelShufflePattern)
    {
        byte b0 = (byte)lePixelShufflePattern;
        byte b1 = (byte)(lePixelShufflePattern >> 8);
        byte b2 = (byte)(lePixelShufflePattern >> 16);

        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                byte c0 = b0 == 0 ? p.R : b0 == 1 ? p.G : p.B;
                byte c1 = b1 == 0 ? p.R : b1 == 1 ? p.G : p.B;
                byte c2 = b2 == 0 ? p.R : b2 == 1 ? p.G : p.B;
                slice[k] = new PixelData(c0, c1, c2, p.A);
            }
            return;
        }

        Span<byte> bytes = MemoryMarshal.AsBytes(slice);
        ref byte ptr = ref MemoryMarshal.GetReference(bytes);
        int byteLen = bytes.Length;
        int i = 0;

        Vector128<byte> mask128 = Vector128.Create(
            b0, b1, b2, (byte)3,
            (byte)(b0 + 4), (byte)(b1 + 4), (byte)(b2 + 4), (byte)7,
            (byte)(b0 + 8), (byte)(b1 + 8), (byte)(b2 + 8), (byte)11,
            (byte)(b0 + 12), (byte)(b1 + 12), (byte)(b2 + 12), (byte)15);

        if (Avx512BW.IsSupported && byteLen >= Vector512<byte>.Count)
        {
            Vector256<byte> mask256 = Vector256.Create(mask128, mask128);
            Vector512<byte> mask512 = Vector512.Create(mask256, mask256);
            int end = byteLen - Vector512<byte>.Count;
            for (; i <= end; i += Vector512<byte>.Count)
            {
                Vector512<byte> v = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                Avx512BW.Shuffle(v, mask512).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Avx2.IsSupported && i <= byteLen - Vector256<byte>.Count)
        {
            Vector256<byte> mask256 = Vector256.Create(mask128, mask128);
            int end = byteLen - Vector256<byte>.Count;
            for (; i <= end; i += Vector256<byte>.Count)
            {
                Vector256<byte> v = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                Avx2.Shuffle(v, mask256).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= byteLen - Vector128<byte>.Count)
        {
            int end = byteLen - Vector128<byte>.Count;
            for (; i <= end; i += Vector128<byte>.Count)
            {
                Vector128<byte> v = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                Vector128.Shuffle(v, mask128).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < byteLen; i += 4)
        {
            byte r = Unsafe.Add(ref ptr, i);
            byte g = Unsafe.Add(ref ptr, i + 1);
            byte b = Unsafe.Add(ref ptr, i + 2);
            Unsafe.Add(ref ptr, i) = b0 == 0 ? r : b0 == 1 ? g : b;
            Unsafe.Add(ref ptr, i + 1) = b1 == 0 ? r : b1 == 1 ? g : b;
            Unsafe.Add(ref ptr, i + 2) = b2 == 0 ? r : b2 == 1 ? g : b;
        }
    }

    public static void ConvertToGrayscale(Span<PixelData> slice)
    {
        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                byte g = ColorCalculator.ToGrayscale(p);
                slice[k] = new PixelData(g, g, g, p.A);
            }
            return;
        }

        Span<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
        ref uint ptr = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Vector512.IsHardwareAccelerated && len >= Vector512<uint>.Count)
        {
            var cMask = Vector512.Create(0xFFu);
            var aMask = Vector512.Create(0xFF000000u);
            var wR = Vector512.Create(9798u);
            var wG = Vector512.Create(19235u);
            var wB = Vector512.Create(3735u);
            var splat = Vector512.Create(0x00010101u);

            int end = len - Vector512<uint>.Count;
            for (; i <= end; i += Vector512<uint>.Count)
            {
                Vector512<uint> px = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                Vector512<uint> r = px & cMask;
                Vector512<uint> g = (px >> 8) & cMask;
                Vector512<uint> b = (px >> 16) & cMask;
                Vector512<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                ((px & aMask) | (gray * splat)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector256.IsHardwareAccelerated && i <= len - Vector256<uint>.Count)
        {
            var cMask = Vector256.Create(0xFFu);
            var aMask = Vector256.Create(0xFF000000u);
            var wR = Vector256.Create(9798u);
            var wG = Vector256.Create(19235u);
            var wB = Vector256.Create(3735u);
            var splat = Vector256.Create(0x00010101u);

            int end = len - Vector256<uint>.Count;
            for (; i <= end; i += Vector256<uint>.Count)
            {
                Vector256<uint> px = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                Vector256<uint> r = px & cMask;
                Vector256<uint> g = (px >> 8) & cMask;
                Vector256<uint> b = (px >> 16) & cMask;
                Vector256<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                ((px & aMask) | (gray * splat)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - Vector128<uint>.Count)
        {
            var cMask = Vector128.Create(0xFFu);
            var aMask = Vector128.Create(0xFF000000u);
            var wR = Vector128.Create(9798u);
            var wG = Vector128.Create(19235u);
            var wB = Vector128.Create(3735u);
            var splat = Vector128.Create(0x00010101u);

            int end = len - Vector128<uint>.Count;
            for (; i <= end; i += Vector128<uint>.Count)
            {
                Vector128<uint> px = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                Vector128<uint> r = px & cMask;
                Vector128<uint> g = (px >> 8) & cMask;
                Vector128<uint> b = (px >> 16) & cMask;
                Vector128<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                ((px & aMask) | (gray * splat)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < len; i++)
        {
            ref uint pxRef = ref Unsafe.Add(ref ptr, i);
            uint px = pxRef;
            uint r = px & 0xFFu;
            uint g = (px >> 8) & 0xFFu;
            uint b = (px >> 16) & 0xFFu;
            uint gray = (9798u * r + 19235u * g + 3735u * b) >> 15;
            pxRef = (px & 0xFF000000u) | (gray * 0x00010101u);
        }
    }

    public static void ConvertToSepia(Span<PixelData> slice)
    {
        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                uint nr = Math.Min(255u, (12878u * p.R + 25199u * p.G + 6193u * p.B) >> 15);
                uint ng = Math.Min(255u, (11436u * p.R + 22479u * p.G + 5505u * p.B) >> 15);
                uint nb = Math.Min(255u, (8913u * p.R + 17498u * p.G + 4293u * p.B) >> 15);
                slice[k] = new PixelData((byte)nr, (byte)ng, (byte)nb, p.A);
            }
            return;
        }

        Span<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
        ref uint ptr = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Vector512.IsHardwareAccelerated && len >= Vector512<uint>.Count)
        {
            var cMask = Vector512.Create(0xFFu);
            var aMask = Vector512.Create(0xFF000000u);
            var max255 = Vector512.Create(255u);
            var r0 = Vector512.Create(12878u); var r1 = Vector512.Create(25199u); var r2 = Vector512.Create(6193u);
            var g0 = Vector512.Create(11436u); var g1 = Vector512.Create(22479u); var g2 = Vector512.Create(5505u);
            var b0 = Vector512.Create(8913u);  var b1 = Vector512.Create(17498u); var b2 = Vector512.Create(4293u);

            int end = len - Vector512<uint>.Count;
            for (; i <= end; i += Vector512<uint>.Count)
            {
                Vector512<uint> px = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                Vector512<uint> r = px & cMask;
                Vector512<uint> g = (px >> 8) & cMask;
                Vector512<uint> b = (px >> 16) & cMask;
                Vector512<uint> nr = Vector512.Min((r * r0 + g * r1 + b * r2) >> 15, max255);
                Vector512<uint> ng = Vector512.Min((r * g0 + g * g1 + b * g2) >> 15, max255);
                Vector512<uint> nb = Vector512.Min((r * b0 + g * b1 + b * b2) >> 15, max255);
                ((px & aMask) | nr | (ng << 8) | (nb << 16)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector256.IsHardwareAccelerated && i <= len - Vector256<uint>.Count)
        {
            var cMask = Vector256.Create(0xFFu);
            var aMask = Vector256.Create(0xFF000000u);
            var max255 = Vector256.Create(255u);
            var r0 = Vector256.Create(12878u); var r1 = Vector256.Create(25199u); var r2 = Vector256.Create(6193u);
            var g0 = Vector256.Create(11436u); var g1 = Vector256.Create(22479u); var g2 = Vector256.Create(5505u);
            var b0 = Vector256.Create(8913u);  var b1 = Vector256.Create(17498u); var b2 = Vector256.Create(4293u);

            int end = len - Vector256<uint>.Count;
            for (; i <= end; i += Vector256<uint>.Count)
            {
                Vector256<uint> px = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                Vector256<uint> r = px & cMask;
                Vector256<uint> g = (px >> 8) & cMask;
                Vector256<uint> b = (px >> 16) & cMask;
                Vector256<uint> nr = Vector256.Min((r * r0 + g * r1 + b * r2) >> 15, max255);
                Vector256<uint> ng = Vector256.Min((r * g0 + g * g1 + b * g2) >> 15, max255);
                Vector256<uint> nb = Vector256.Min((r * b0 + g * b1 + b * b2) >> 15, max255);
                ((px & aMask) | nr | (ng << 8) | (nb << 16)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - Vector128<uint>.Count)
        {
            var cMask = Vector128.Create(0xFFu);
            var aMask = Vector128.Create(0xFF000000u);
            var max255 = Vector128.Create(255u);
            var r0 = Vector128.Create(12878u); var r1 = Vector128.Create(25199u); var r2 = Vector128.Create(6193u);
            var g0 = Vector128.Create(11436u); var g1 = Vector128.Create(22479u); var g2 = Vector128.Create(5505u);
            var b0 = Vector128.Create(8913u);  var b1 = Vector128.Create(17498u); var b2 = Vector128.Create(4293u);

            int end = len - Vector128<uint>.Count;
            for (; i <= end; i += Vector128<uint>.Count)
            {
                Vector128<uint> px = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                Vector128<uint> r = px & cMask;
                Vector128<uint> g = (px >> 8) & cMask;
                Vector128<uint> b = (px >> 16) & cMask;
                Vector128<uint> nr = Vector128.Min((r * r0 + g * r1 + b * r2) >> 15, max255);
                Vector128<uint> ng = Vector128.Min((r * g0 + g * g1 + b * g2) >> 15, max255);
                Vector128<uint> nb = Vector128.Min((r * b0 + g * b1 + b * b2) >> 15, max255);
                ((px & aMask) | nr | (ng << 8) | (nb << 16)).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < len; i++)
        {
            ref uint pxRef = ref Unsafe.Add(ref ptr, i);
            uint px = pxRef;
            uint r = px & 0xFFu;
            uint g = (px >> 8) & 0xFFu;
            uint b = (px >> 16) & 0xFFu;
            uint nr = Math.Min(255u, (12878u * r + 25199u * g + 6193u * b) >> 15);
            uint ng = Math.Min(255u, (11436u * r + 22479u * g + 5505u * b) >> 15);
            uint nb = Math.Min(255u, (8913u * r + 17498u * g + 4293u * b) >> 15);
            pxRef = (px & 0xFF000000u) | nr | (ng << 8) | (nb << 16);
        }
    }

    public static void ApplyBinaryThreshold(Span<PixelData> slice, int threshold)
    {
        uint uthreshold = (uint)threshold;
        if (!BitConverter.IsLittleEndian)
        {
            for (int k = 0; k < slice.Length; k++)
            {
                var p = slice[k];
                byte val = ColorCalculator.ToGrayscale(p) < uthreshold ? (byte)0 : (byte)255;
                slice[k] = new PixelData(val, val, val, p.A);
            }
            return;
        }

        Span<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
        ref uint ptr = ref MemoryMarshal.GetReference(data);
        int len = data.Length;
        int i = 0;

        if (Vector512.IsHardwareAccelerated && len >= Vector512<uint>.Count)
        {
            var cMask = Vector512.Create(0xFFu);
            var aMask = Vector512.Create(0xFF000000u);
            var whiteRgb = Vector512.Create(0x00FFFFFFu);
            var thresh = Vector512.Create(uthreshold);
            var wR = Vector512.Create(9798u);
            var wG = Vector512.Create(19235u);
            var wB = Vector512.Create(3735u);

            int end = len - Vector512<uint>.Count;
            for (; i <= end; i += Vector512<uint>.Count)
            {
                Vector512<uint> px = Vector512.LoadUnsafe(ref ptr, (nuint)i);
                Vector512<uint> r = px & cMask;
                Vector512<uint> g = (px >> 8) & cMask;
                Vector512<uint> b = (px >> 16) & cMask;
                Vector512<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                Vector512<uint> rgb = Vector512.GreaterThanOrEqual(gray, thresh) & whiteRgb;
                ((px & aMask) | rgb).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector256.IsHardwareAccelerated && i <= len - Vector256<uint>.Count)
        {
            var cMask = Vector256.Create(0xFFu);
            var aMask = Vector256.Create(0xFF000000u);
            var whiteRgb = Vector256.Create(0x00FFFFFFu);
            var thresh = Vector256.Create(uthreshold);
            var wR = Vector256.Create(9798u);
            var wG = Vector256.Create(19235u);
            var wB = Vector256.Create(3735u);

            int end = len - Vector256<uint>.Count;
            for (; i <= end; i += Vector256<uint>.Count)
            {
                Vector256<uint> px = Vector256.LoadUnsafe(ref ptr, (nuint)i);
                Vector256<uint> r = px & cMask;
                Vector256<uint> g = (px >> 8) & cMask;
                Vector256<uint> b = (px >> 16) & cMask;
                Vector256<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                Vector256<uint> rgb = Vector256.GreaterThanOrEqual(gray, thresh) & whiteRgb;
                ((px & aMask) | rgb).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - Vector128<uint>.Count)
        {
            var cMask = Vector128.Create(0xFFu);
            var aMask = Vector128.Create(0xFF000000u);
            var whiteRgb = Vector128.Create(0x00FFFFFFu);
            var thresh = Vector128.Create(uthreshold);
            var wR = Vector128.Create(9798u);
            var wG = Vector128.Create(19235u);
            var wB = Vector128.Create(3735u);

            int end = len - Vector128<uint>.Count;
            for (; i <= end; i += Vector128<uint>.Count)
            {
                Vector128<uint> px = Vector128.LoadUnsafe(ref ptr, (nuint)i);
                Vector128<uint> r = px & cMask;
                Vector128<uint> g = (px >> 8) & cMask;
                Vector128<uint> b = (px >> 16) & cMask;
                Vector128<uint> gray = (r * wR + g * wG + b * wB) >> 15;
                Vector128<uint> rgb = Vector128.GreaterThanOrEqual(gray, thresh) & whiteRgb;
                ((px & aMask) | rgb).StoreUnsafe(ref ptr, (nuint)i);
            }
        }

        for (; i < len; i++)
        {
            ref uint pxRef = ref Unsafe.Add(ref ptr, i);
            uint px = pxRef;
            uint r = px & 0xFFu;
            uint g = (px >> 8) & 0xFFu;
            uint b = (px >> 16) & 0xFFu;
            uint gray = (9798u * r + 19235u * g + 3735u * b) >> 15;
            uint rgb = gray < uthreshold ? 0u : 0x00FFFFFFu;
            pxRef = (px & 0xFF000000u) | rgb;
        }
    }

    public static void AccumulateHistogram(ReadOnlySpan<PixelData> slice, Span<int> histogram)
    {
        // 4 independent 256-bin lanes avoid store-to-load forwarding stalls on smooth regions.
        Span<int> lanes = stackalloc int[256 * 4];
        lanes.Clear();
        ref int h0 = ref MemoryMarshal.GetReference(lanes);
        ref int h1 = ref Unsafe.Add(ref h0, 256);
        ref int h2 = ref Unsafe.Add(ref h0, 512);
        ref int h3 = ref Unsafe.Add(ref h0, 768);

        if (BitConverter.IsLittleEndian)
        {
            ReadOnlySpan<uint> data = MemoryMarshal.Cast<PixelData, uint>(slice);
            ref uint ptr = ref MemoryMarshal.GetReference(data);
            int len = data.Length;
            int i = 0;

            for (; i <= len - 4; i += 4)
            {
                uint p0 = Unsafe.Add(ref ptr, i);
                uint p1 = Unsafe.Add(ref ptr, i + 1);
                uint p2 = Unsafe.Add(ref ptr, i + 2);
                uint p3 = Unsafe.Add(ref ptr, i + 3);

                uint g0 = (9798u * (p0 & 0xFFu) + 19235u * ((p0 >> 8) & 0xFFu) + 3735u * ((p0 >> 16) & 0xFFu)) >> 15;
                uint g1 = (9798u * (p1 & 0xFFu) + 19235u * ((p1 >> 8) & 0xFFu) + 3735u * ((p1 >> 16) & 0xFFu)) >> 15;
                uint g2 = (9798u * (p2 & 0xFFu) + 19235u * ((p2 >> 8) & 0xFFu) + 3735u * ((p2 >> 16) & 0xFFu)) >> 15;
                uint g3 = (9798u * (p3 & 0xFFu) + 19235u * ((p3 >> 8) & 0xFFu) + 3735u * ((p3 >> 16) & 0xFFu)) >> 15;

                Unsafe.Add(ref h0, (nuint)g0)++;
                Unsafe.Add(ref h1, (nuint)g1)++;
                Unsafe.Add(ref h2, (nuint)g2)++;
                Unsafe.Add(ref h3, (nuint)g3)++;
            }

            for (; i < len; i++)
            {
                uint p = Unsafe.Add(ref ptr, i);
                uint g = (9798u * (p & 0xFFu) + 19235u * ((p >> 8) & 0xFFu) + 3735u * ((p >> 16) & 0xFFu)) >> 15;
                Unsafe.Add(ref h0, (nuint)g)++;
            }
        }
        else
        {
            for (int i = 0; i < slice.Length; i++)
            {
                byte g = ColorCalculator.ToGrayscale(slice[i]);
                Unsafe.Add(ref h0, g)++;
            }
        }

        ref int dst = ref MemoryMarshal.GetReference(histogram);
        if (Vector512.IsHardwareAccelerated)
        {
            for (int b = 0; b < 256; b += Vector512<int>.Count)
            {
                var sum = Vector512.LoadUnsafe(ref dst, (nuint)b)
                        + Vector512.LoadUnsafe(ref h0, (nuint)b)
                        + Vector512.LoadUnsafe(ref h1, (nuint)b)
                        + Vector512.LoadUnsafe(ref h2, (nuint)b)
                        + Vector512.LoadUnsafe(ref h3, (nuint)b);
                sum.StoreUnsafe(ref dst, (nuint)b);
            }
        }
        else if (Vector256.IsHardwareAccelerated)
        {
            for (int b = 0; b < 256; b += Vector256<int>.Count)
            {
                var sum = Vector256.LoadUnsafe(ref dst, (nuint)b)
                        + Vector256.LoadUnsafe(ref h0, (nuint)b)
                        + Vector256.LoadUnsafe(ref h1, (nuint)b)
                        + Vector256.LoadUnsafe(ref h2, (nuint)b)
                        + Vector256.LoadUnsafe(ref h3, (nuint)b);
                sum.StoreUnsafe(ref dst, (nuint)b);
            }
        }
        else
        {
            for (int b = 0; b < 256; b++)
                Unsafe.Add(ref dst, b) += Unsafe.Add(ref h0, b) + Unsafe.Add(ref h1, b) + Unsafe.Add(ref h2, b) + Unsafe.Add(ref h3, b);
        }
    }
}

