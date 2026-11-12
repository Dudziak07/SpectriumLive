using SpectriumLive.Core.Entities;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SpectriumLive.Application.Helpers;

internal static class ConvolutionSimdHelper
{
    private const int ParallelRowWorkThreshold = 65_536;

    internal static (int BandCount, PixelData[][]? Seams) PrepareBands(
        PixelData[] pixels,
        int width,
        int height,
        int radius,
        out int interiorStart,
        out int interiorRows,
        out int rowsPerBand,
        out int remainderRows)
    {
        interiorStart = radius;
        interiorRows = height - 2 * radius;
        if (interiorRows <= 0 || width <= 2 * radius)
        {
            rowsPerBand = 0;
            remainderRows = 0;
            return (0, null);
        }

        int bandCount;
        if (interiorRows * width < ParallelRowWorkThreshold || Environment.ProcessorCount <= 1 || interiorRows < Math.Max(2, radius) * 2)
        {
            bandCount = 1;
        }
        else
        {
            int maxBands = interiorRows / Math.Max(radius, 8);
            bandCount = Math.Clamp(maxBands, 1, Environment.ProcessorCount);
        }

        rowsPerBand = interiorRows / bandCount;
        remainderRows = interiorRows % bandCount;

        if (bandCount <= 1)
            return (1, null);

        var seams = new PixelData[bandCount - 1][];
        int seamRows = 2 * radius;
        int seamElements = seamRows * width;
        int cursor = interiorStart;

        for (int b = 0; b < bandCount - 1; b++)
        {
            int bandRows = rowsPerBand + (b < remainderRows ? 1 : 0);
            int seamY = cursor + bandRows;
            cursor = seamY;

            var rented = ArrayPool<PixelData>.Shared.Rent(seamElements);
            pixels.AsSpan((seamY - radius) * width, seamElements).CopyTo(rented.AsSpan(0, seamElements));
            seams[b] = rented;
        }

        return (bandCount, seams);
    }

    internal static void ReturnSeams(PixelData[][]? seams)
    {
        if (seams == null)
            return;

        for (int i = 0; i < seams.Length; i++)
        {
            if (seams[i] != null)
                ArrayPool<PixelData>.Shared.Return(seams[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void GetBandBounds(int bandIndex, int interiorStart, int rowsPerBand, int remainderRows, out int y0, out int y1)
    {
        int offset = bandIndex * rowsPerBand + Math.Min(bandIndex, remainderRows);
        int count = rowsPerBand + (bandIndex < remainderRows ? 1 : 0);
        y0 = interiorStart + offset;
        y1 = y0 + count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<PixelData> GetSourceRow(
        PixelData[] pixels,
        PixelData[][]? seams,
        int width,
        int radius,
        int bandIndex,
        int bandCount,
        int y0,
        int y1,
        int srcY)
    {
        if (seams != null)
        {
            if (bandIndex > 0 && srcY < y0)
                return seams[bandIndex - 1].AsSpan((srcY - (y0 - radius)) * width, width);

            if (bandIndex < bandCount - 1 && srcY >= y1)
                return seams[bandIndex].AsSpan((radius + (srcY - y1)) * width, width);
        }

        return pixels.AsSpan(srcY * width, width);
    }

    public static void ApplyBoxBlur3x3(ImageBuffer imageBuffer) =>
        Apply3x3NeighborhoodFilter(imageBuffer, isEdgeDetection: false);

    public static void ApplyEdgeDetection3x3(ImageBuffer imageBuffer) =>
        Apply3x3NeighborhoodFilter(imageBuffer, isEdgeDetection: true);

    private static void Apply3x3NeighborhoodFilter(ImageBuffer imageBuffer, bool isEdgeDetection)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;
        var pixels = imageBuffer.Pixels;
        if (width < 3 || height < 3 || pixels.Length < width * height)
            return;

        var (bandCount, seams) = PrepareBands(pixels, width, height, 1,
            out int interiorStart, out int interiorRows, out int rowsPerBand, out int remainderRows);
        if (bandCount == 0)
            return;

        try
        {
            if (bandCount == 1)
            {
                Process3x3Band(pixels, null, width, bandIndex: 0, bandCount: 1, interiorStart, interiorStart + interiorRows, isEdgeDetection);
            }
            else
            {
                Parallel.For(0, bandCount, bandIndex =>
                {
                    GetBandBounds(bandIndex, interiorStart, rowsPerBand, remainderRows, out int y0, out int y1);
                    Process3x3Band(pixels, seams, width, bandIndex, bandCount, y0, y1, isEdgeDetection);
                });
            }
        }
        finally
        {
            ReturnSeams(seams);
        }
    }

    public static void ApplySeparableGaussianBlur(ImageBuffer imageBuffer, ReadOnlySpan<float> kernel1D)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;
        int kernelSize = kernel1D.Length;
        int radius = kernelSize / 2;
        var pixels = imageBuffer.Pixels;

        if (width < kernelSize || height < kernelSize || pixels.Length < width * height || radius == 0)
            return;

        var (bandCount, seams) = PrepareBands(pixels, width, height, radius,
            out int interiorStart, out int interiorRows, out int rowsPerBand, out int remainderRows);
        if (bandCount == 0)
            return;

        float[] kernelArray = kernel1D.ToArray();

        try
        {
            if (bandCount == 1)
            {
                ProcessGaussianBand(pixels, null, width, kernelSize, radius, kernelArray, 0, 1, interiorStart, interiorStart + interiorRows);
            }
            else
            {
                Parallel.For(0, bandCount, bandIndex =>
                {
                    GetBandBounds(bandIndex, interiorStart, rowsPerBand, remainderRows, out int y0, out int y1);
                    ProcessGaussianBand(pixels, seams, width, kernelSize, radius, kernelArray, bandIndex, bandCount, y0, y1);
                });
            }
        }
        finally
        {
            ReturnSeams(seams);
        }
    }

    private static void Process3x3Band(
        PixelData[] pixels,
        PixelData[][]? seams,
        int width,
        int bandIndex,
        int bandCount,
        int y0,
        int y1,
        bool isEdgeDetection)
    {
        int rowUshorts = width * 4;
        ushort[] ring = ArrayPool<ushort>.Shared.Rent(rowUshorts * 3);
        try
        {
            var h0 = ring.AsSpan(0, rowUshorts);
            var h1 = ring.AsSpan(rowUshorts, rowUshorts);
            var h2 = ring.AsSpan(rowUshorts * 2, rowUshorts);

            ComputeHorizontalSum3(GetSourceRow(pixels, seams, width, 1, bandIndex, bandCount, y0, y1, y0 - 1), h0, width);
            ComputeHorizontalSum3(GetSourceRow(pixels, seams, width, 1, bandIndex, bandCount, y0, y1, y0), h1, width);

            for (int y = y0; y < y1; y++)
            {
                ComputeHorizontalSum3(GetSourceRow(pixels, seams, width, 1, bandIndex, bandCount, y0, y1, y + 1), h2, width);
                var dstRow = pixels.AsSpan(y * width, width);

                if (isEdgeDetection)
                    CombineEdgeRow(h0, h1, h2, dstRow, width);
                else
                    CombineBoxBlurRow(h0, h1, h2, dstRow, width);

                var tmp = h0;
                h0 = h1;
                h1 = h2;
                h2 = tmp;
            }
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(ring);
        }
    }

    private static void ComputeHorizontalSum3(ReadOnlySpan<PixelData> srcRow, Span<ushort> dstUshorts, int width)
    {
        ReadOnlySpan<byte> srcBytes = MemoryMarshal.AsBytes(srcRow);
        ref byte srcRef = ref MemoryMarshal.GetReference(srcBytes);
        ref ushort dstRef = ref MemoryMarshal.GetReference(dstUshorts);

        int x = 1;
        int end = width - 1;

        if (Avx512BW.IsSupported && end - x >= 8)
        {
            int simdEnd = end - 8;
            for (; x <= simdEnd; x += 8)
            {
                int bOff = (x - 1) * 4;
                Vector512<ushort> left = Avx512BW.ConvertToVector512Int16(Vector256.LoadUnsafe(ref srcRef, (nuint)bOff)).AsUInt16();
                Vector512<ushort> mid = Avx512BW.ConvertToVector512Int16(Vector256.LoadUnsafe(ref srcRef, (nuint)(bOff + 4))).AsUInt16();
                Vector512<ushort> right = Avx512BW.ConvertToVector512Int16(Vector256.LoadUnsafe(ref srcRef, (nuint)(bOff + 8))).AsUInt16();
                (left + mid + right).StoreUnsafe(ref dstRef, (nuint)(x * 4));
            }
        }

        if (Avx2.IsSupported && end - x >= 4)
        {
            int simdEnd = end - 4;
            for (; x <= simdEnd; x += 4)
            {
                int bOff = (x - 1) * 4;
                Vector256<ushort> left = Avx2.ConvertToVector256Int16(Vector128.LoadUnsafe(ref srcRef, (nuint)bOff)).AsUInt16();
                Vector256<ushort> mid = Avx2.ConvertToVector256Int16(Vector128.LoadUnsafe(ref srcRef, (nuint)(bOff + 4))).AsUInt16();
                Vector256<ushort> right = Avx2.ConvertToVector256Int16(Vector128.LoadUnsafe(ref srcRef, (nuint)(bOff + 8))).AsUInt16();
                (left + mid + right).StoreUnsafe(ref dstRef, (nuint)(x * 4));
            }
        }

        for (; x < end; x++)
        {
            int u = x * 4;
            int b = (x - 1) * 4;
            Unsafe.Add(ref dstRef, u) = (ushort)(Unsafe.Add(ref srcRef, b) + Unsafe.Add(ref srcRef, b + 4) + Unsafe.Add(ref srcRef, b + 8));
            Unsafe.Add(ref dstRef, u + 1) = (ushort)(Unsafe.Add(ref srcRef, b + 1) + Unsafe.Add(ref srcRef, b + 5) + Unsafe.Add(ref srcRef, b + 9));
            Unsafe.Add(ref dstRef, u + 2) = (ushort)(Unsafe.Add(ref srcRef, b + 2) + Unsafe.Add(ref srcRef, b + 6) + Unsafe.Add(ref srcRef, b + 10));
        }
    }

    private static void CombineBoxBlurRow(
        ReadOnlySpan<ushort> h0,
        ReadOnlySpan<ushort> h1,
        ReadOnlySpan<ushort> h2,
        Span<PixelData> rowPixels,
        int width)
    {
        ref ushort r0 = ref MemoryMarshal.GetReference(h0);
        ref ushort r1 = ref MemoryMarshal.GetReference(h1);
        ref ushort r2 = ref MemoryMarshal.GetReference(h2);
        Span<byte> dstBytes = MemoryMarshal.AsBytes(rowPixels);
        ref byte dstRef = ref MemoryMarshal.GetReference(dstBytes);

        int x = 1;
        int end = width - 1;

        if (Avx512BW.IsSupported && BitConverter.IsLittleEndian && end - x >= 8)
        {
            var inv9 = Vector512.Create((ushort)7282); // (sum * 7282) >> 16 matches sum / 9 for [0..2295]
            var rgbMask = Vector256.Create(0x00FFFFFFu).AsByte();
            var aMask = Vector256.Create(0xFF000000u).AsByte();
            int simdEnd = end - 8;

            for (; x <= simdEnd; x += 8)
            {
                int offset = x * 4; // 4 ushorts and 4 bytes per RGBA pixel
                Vector512<ushort> sum = Vector512.LoadUnsafe(ref r0, (nuint)offset)
                                      + Vector512.LoadUnsafe(ref r1, (nuint)offset)
                                      + Vector512.LoadUnsafe(ref r2, (nuint)offset);
                Vector512<ushort> avg = Avx512BW.MultiplyHigh(sum, inv9);
                Vector256<byte> packed = Vector512.Narrow(avg, avg).GetLower();
                Vector256<byte> orig = Vector256.LoadUnsafe(ref dstRef, (nuint)offset);
                ((packed & rgbMask) | (orig & aMask)).StoreUnsafe(ref dstRef, (nuint)offset);
            }
        }

        if (Avx2.IsSupported && BitConverter.IsLittleEndian && end - x >= 4)
        {
            var inv9 = Vector256.Create((ushort)7282);
            var rgbMask = Vector128.Create(0x00FFFFFFu).AsByte();
            var aMask = Vector128.Create(0xFF000000u).AsByte();
            int simdEnd = end - 4;

            for (; x <= simdEnd; x += 4)
            {
                int offset = x * 4;
                Vector256<ushort> sum = Vector256.LoadUnsafe(ref r0, (nuint)offset)
                                      + Vector256.LoadUnsafe(ref r1, (nuint)offset)
                                      + Vector256.LoadUnsafe(ref r2, (nuint)offset);
                Vector256<ushort> avg = Avx2.MultiplyHigh(sum, inv9);
                Vector128<byte> packed = Vector256.Narrow(avg, avg).GetLower();
                Vector128<byte> orig = Vector128.LoadUnsafe(ref dstRef, (nuint)offset);
                ((packed & rgbMask) | (orig & aMask)).StoreUnsafe(ref dstRef, (nuint)offset);
            }
        }

        for (; x < end; x++)
        {
            int u = x * 4;
            byte rr = (byte)(((Unsafe.Add(ref r0, u) + Unsafe.Add(ref r1, u) + Unsafe.Add(ref r2, u)) * 7282u) >> 16);
            byte gg = (byte)(((Unsafe.Add(ref r0, u + 1) + Unsafe.Add(ref r1, u + 1) + Unsafe.Add(ref r2, u + 1)) * 7282u) >> 16);
            byte bb = (byte)(((Unsafe.Add(ref r0, u + 2) + Unsafe.Add(ref r1, u + 2) + Unsafe.Add(ref r2, u + 2)) * 7282u) >> 16);
            Unsafe.Add(ref dstRef, u) = rr;
            Unsafe.Add(ref dstRef, u + 1) = gg;
            Unsafe.Add(ref dstRef, u + 2) = bb;
        }
    }

    private static void CombineEdgeRow(
        ReadOnlySpan<ushort> h0,
        ReadOnlySpan<ushort> h1,
        ReadOnlySpan<ushort> h2,
        Span<PixelData> rowPixels,
        int width)
    {
        ref ushort r0 = ref MemoryMarshal.GetReference(h0);
        ref ushort r1 = ref MemoryMarshal.GetReference(h1);
        ref ushort r2 = ref MemoryMarshal.GetReference(h2);
        Span<byte> dstBytes = MemoryMarshal.AsBytes(rowPixels);
        ref byte dstRef = ref MemoryMarshal.GetReference(dstBytes);

        int x = 1;
        int end = width - 1;

        if (Avx512BW.IsSupported && BitConverter.IsLittleEndian && end - x >= 8)
        {
            var nine = Vector512.Create((ushort)9);
            var max255 = Vector512.Create((ushort)255);
            var rgbMask = Vector256.Create(0x00FFFFFFu).AsByte();
            var aMask = Vector256.Create(0xFF000000u).AsByte();
            int simdEnd = end - 8;

            for (; x <= simdEnd; x += 8)
            {
                int offset = x * 4;
                Vector256<byte> orig = Vector256.LoadUnsafe(ref dstRef, (nuint)offset);
                Vector512<ushort> boxSum = Vector512.LoadUnsafe(ref r0, (nuint)offset)
                                         + Vector512.LoadUnsafe(ref r1, (nuint)offset)
                                         + Vector512.LoadUnsafe(ref r2, (nuint)offset);
                Vector512<ushort> center9 = Avx512BW.ConvertToVector512Int16(orig).AsUInt16() * nine;
                Vector512<ushort> diff = Vector512.Min(Vector512.Max(center9, boxSum) - Vector512.Min(center9, boxSum), max255);
                Vector256<byte> packed = Vector512.Narrow(diff, diff).GetLower();
                ((packed & rgbMask) | (orig & aMask)).StoreUnsafe(ref dstRef, (nuint)offset);
            }
        }

        if (Avx2.IsSupported && BitConverter.IsLittleEndian && end - x >= 4)
        {
            var nine = Vector256.Create((ushort)9);
            var max255 = Vector256.Create((ushort)255);
            var rgbMask = Vector128.Create(0x00FFFFFFu).AsByte();
            var aMask = Vector128.Create(0xFF000000u).AsByte();
            int simdEnd = end - 4;

            for (; x <= simdEnd; x += 4)
            {
                int offset = x * 4;
                Vector128<byte> orig = Vector128.LoadUnsafe(ref dstRef, (nuint)offset);
                Vector256<ushort> boxSum = Vector256.LoadUnsafe(ref r0, (nuint)offset)
                                         + Vector256.LoadUnsafe(ref r1, (nuint)offset)
                                         + Vector256.LoadUnsafe(ref r2, (nuint)offset);
                Vector256<ushort> center9 = Avx2.ConvertToVector256Int16(orig).AsUInt16() * nine;
                Vector256<ushort> diff = Vector256.Min(Vector256.Max(center9, boxSum) - Vector256.Min(center9, boxSum), max255);
                Vector128<byte> packed = Vector256.Narrow(diff, diff).GetLower();
                ((packed & rgbMask) | (orig & aMask)).StoreUnsafe(ref dstRef, (nuint)offset);
            }
        }

        for (; x < end; x++)
        {
            int u = x * 4;
            int dr = Math.Abs(9 * Unsafe.Add(ref dstRef, u) - (Unsafe.Add(ref r0, u) + Unsafe.Add(ref r1, u) + Unsafe.Add(ref r2, u)));
            int dg = Math.Abs(9 * Unsafe.Add(ref dstRef, u + 1) - (Unsafe.Add(ref r0, u + 1) + Unsafe.Add(ref r1, u + 1) + Unsafe.Add(ref r2, u + 1)));
            int db = Math.Abs(9 * Unsafe.Add(ref dstRef, u + 2) - (Unsafe.Add(ref r0, u + 2) + Unsafe.Add(ref r1, u + 2) + Unsafe.Add(ref r2, u + 2)));
            Unsafe.Add(ref dstRef, u) = (byte)Math.Min(255, dr);
            Unsafe.Add(ref dstRef, u + 1) = (byte)Math.Min(255, dg);
            Unsafe.Add(ref dstRef, u + 2) = (byte)Math.Min(255, db);
        }
    }

    private static void ProcessGaussianBand(
        PixelData[] pixels,
        PixelData[][]? seams,
        int width,
        int kernelSize,
        int radius,
        float[] kernel1D,
        int bandIndex,
        int bandCount,
        int y0,
        int y1)
    {
        int rowFloats = width * 4;
        float[] ring = ArrayPool<float>.Shared.Rent(kernelSize * rowFloats);
        try
        {
            for (int i = 0; i < kernelSize - 1; i++)
            {
                int srcY = y0 - radius + i;
                var srcRow = GetSourceRow(pixels, seams, width, radius, bandIndex, bandCount, y0, y1, srcY);
                HorizontalGaussianRow(srcRow, ring.AsSpan(i * rowFloats, rowFloats), width, radius, kernel1D);
            }

            int nextSlot = kernelSize - 1;
            int centerSlot = radius;

            for (int y = y0; y < y1; y++)
            {
                var srcRow = GetSourceRow(pixels, seams, width, radius, bandIndex, bandCount, y0, y1, y + radius);
                HorizontalGaussianRow(srcRow, ring.AsSpan(nextSlot * rowFloats, rowFloats), width, radius, kernel1D);

                VerticalGaussianRow(
                    ring,
                    rowFloats,
                    kernelSize,
                    centerSlot,
                    radius,
                    kernel1D,
                    pixels.AsSpan(y * width, width),
                    width);

                nextSlot++;
                if (nextSlot == kernelSize)
                    nextSlot = 0;

                centerSlot++;
                if (centerSlot == kernelSize)
                    centerSlot = 0;
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(ring);
        }
    }

    private static void HorizontalGaussianRow(
        ReadOnlySpan<PixelData> srcRow,
        Span<float> dstFloats,
        int width,
        int radius,
        float[] kernel1D)
    {
        ReadOnlySpan<byte> srcBytes = MemoryMarshal.AsBytes(srcRow);
        ref byte srcRef = ref MemoryMarshal.GetReference(srcBytes);
        ref float dstRef = ref MemoryMarshal.GetReference(dstFloats);
        float centerWeight = kernel1D[radius];

        int x = radius;
        int end = width - radius;

        if (Avx512F.IsSupported && end - x >= 4)
        {
            var cw = Vector512.Create(centerWeight);
            int simdEnd = end - 4;

            for (; x <= simdEnd; x += 4)
            {
                int centerByteOffset = x * 4;
                Vector512<float> acc = Vector512.ConvertToSingle(Avx512F.ConvertToVector512Int32(Vector128.LoadUnsafe(ref srcRef, (nuint)centerByteOffset))) * cw;

                for (int k = 1; k <= radius; k++)
                {
                    var wk = Vector512.Create(kernel1D[radius + k]);
                    int kBytes = k * 4;
                    Vector512<float> left = Vector512.ConvertToSingle(Avx512F.ConvertToVector512Int32(Vector128.LoadUnsafe(ref srcRef, (nuint)(centerByteOffset - kBytes))));
                    Vector512<float> right = Vector512.ConvertToSingle(Avx512F.ConvertToVector512Int32(Vector128.LoadUnsafe(ref srcRef, (nuint)(centerByteOffset + kBytes))));
                    acc += wk * (left + right);
                }

                acc.StoreUnsafe(ref dstRef, (nuint)centerByteOffset);
            }
        }

        if (Avx2.IsSupported && end - x >= 2)
        {
            var cw = Vector256.Create(centerWeight);
            int simdEnd = end - 2;

            for (; x <= simdEnd; x += 2)
            {
                int centerByteOffset = x * 4;
                Vector256<float> acc = Vector256.ConvertToSingle(Avx2.ConvertToVector256Int32(Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref srcRef, centerByteOffset))).AsByte())) * cw;

                for (int k = 1; k <= radius; k++)
                {
                    var wk = Vector256.Create(kernel1D[radius + k]);
                    int kBytes = k * 4;
                    Vector256<float> left = Vector256.ConvertToSingle(Avx2.ConvertToVector256Int32(Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref srcRef, centerByteOffset - kBytes))).AsByte()));
                    Vector256<float> right = Vector256.ConvertToSingle(Avx2.ConvertToVector256Int32(Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref srcRef, centerByteOffset + kBytes))).AsByte()));
                    acc += wk * (left + right);
                }

                acc.StoreUnsafe(ref dstRef, (nuint)centerByteOffset);
            }
        }

        for (; x < end; x++)
        {
            int b = x * 4;
            float sumR = Unsafe.Add(ref srcRef, b) * centerWeight;
            float sumG = Unsafe.Add(ref srcRef, b + 1) * centerWeight;
            float sumB = Unsafe.Add(ref srcRef, b + 2) * centerWeight;

            for (int k = 1; k <= radius; k++)
            {
                float w = kernel1D[radius + k];
                int lb = (x - k) * 4;
                int rb = (x + k) * 4;
                sumR += (Unsafe.Add(ref srcRef, lb) + Unsafe.Add(ref srcRef, rb)) * w;
                sumG += (Unsafe.Add(ref srcRef, lb + 1) + Unsafe.Add(ref srcRef, rb + 1)) * w;
                sumB += (Unsafe.Add(ref srcRef, lb + 2) + Unsafe.Add(ref srcRef, rb + 2)) * w;
            }

            Unsafe.Add(ref dstRef, b) = sumR;
            Unsafe.Add(ref dstRef, b + 1) = sumG;
            Unsafe.Add(ref dstRef, b + 2) = sumB;
        }
    }

    private static void VerticalGaussianRow(
        float[] ring,
        int rowFloats,
        int kernelSize,
        int centerSlot,
        int radius,
        float[] kernel1D,
        Span<PixelData> rowPixels,
        int width)
    {
        Span<byte> dstBytes = MemoryMarshal.AsBytes(rowPixels);
        ref byte dstRef = ref MemoryMarshal.GetReference(dstBytes);
        ref float ringBase = ref MemoryMarshal.GetArrayDataReference(ring);
        int centerRowOffset = centerSlot * rowFloats;
        float centerWeight = kernel1D[radius];

        int x = radius;
        int end = width - radius;

        if (Avx512F.IsSupported && BitConverter.IsLittleEndian && end - x >= 4)
        {
            var cw = Vector512.Create(centerWeight);
            var minZero = Vector512.Create(0.0f);
            var max255 = Vector512.Create(255.0f);
            var rgbMask = Vector128.Create(0x00FFFFFFu).AsByte();
            var aMask = Vector128.Create(0xFF000000u).AsByte();
            int simdEnd = end - 4;

            for (; x <= simdEnd; x += 4)
            {
                int fOff = x * 4;
                Vector512<float> acc = Vector512.LoadUnsafe(ref ringBase, (nuint)(centerRowOffset + fOff)) * cw;

                for (int k = 1; k <= radius; k++)
                {
                    int upSlot = centerSlot - k;
                    if (upSlot < 0) upSlot += kernelSize;
                    int dnSlot = centerSlot + k;
                    if (dnSlot >= kernelSize) dnSlot -= kernelSize;

                    var wk = Vector512.Create(kernel1D[radius + k]);
                    Vector512<float> top = Vector512.LoadUnsafe(ref ringBase, (nuint)(upSlot * rowFloats + fOff));
                    Vector512<float> bot = Vector512.LoadUnsafe(ref ringBase, (nuint)(dnSlot * rowFloats + fOff));
                    acc += wk * (top + bot);
                }

                acc = Vector512.Min(Vector512.Max(acc, minZero), max255);
                Vector512<uint> ints = Avx512F.ConvertToVector512Int32WithTruncation(acc).AsUInt32();
                Vector256<ushort> u16 = Vector512.Narrow(ints, ints).GetLower();
                Vector128<byte> packed = Vector256.Narrow(u16, u16).GetLower();
                Vector128<byte> orig = Vector128.LoadUnsafe(ref dstRef, (nuint)fOff);
                ((packed & rgbMask) | (orig & aMask)).StoreUnsafe(ref dstRef, (nuint)fOff);
            }
        }

        if (Avx2.IsSupported && BitConverter.IsLittleEndian && end - x >= 2)
        {
            var cw = Vector256.Create(centerWeight);
            var minZero = Vector256.Create(0.0f);
            var max255 = Vector256.Create(255.0f);
            int simdEnd = end - 2;

            for (; x <= simdEnd; x += 2)
            {
                int fOff = x * 4;
                Vector256<float> acc = Vector256.LoadUnsafe(ref ringBase, (nuint)(centerRowOffset + fOff)) * cw;

                for (int k = 1; k <= radius; k++)
                {
                    int upSlot = centerSlot - k;
                    if (upSlot < 0) upSlot += kernelSize;
                    int dnSlot = centerSlot + k;
                    if (dnSlot >= kernelSize) dnSlot -= kernelSize;

                    var wk = Vector256.Create(kernel1D[radius + k]);
                    Vector256<float> top = Vector256.LoadUnsafe(ref ringBase, (nuint)(upSlot * rowFloats + fOff));
                    Vector256<float> bot = Vector256.LoadUnsafe(ref ringBase, (nuint)(dnSlot * rowFloats + fOff));
                    acc += wk * (top + bot);
                }

                acc = Vector256.Min(Vector256.Max(acc, minZero), max255);
                Vector256<uint> ints = Avx.ConvertToVector256Int32WithTruncation(acc).AsUInt32();
                Vector128<ushort> u16 = Vector256.Narrow(ints, ints).GetLower();
                Vector128<byte> bytes = Vector128.Narrow(u16, u16);

                ulong packed2Px = bytes.AsUInt64().GetElement(0);
                ref ulong pxOut2 = ref Unsafe.As<byte, ulong>(ref Unsafe.Add(ref dstRef, fOff));
                pxOut2 = (packed2Px & 0x00FFFFFF_00FFFFFFul) | (pxOut2 & 0xFF000000_FF000000ul);
            }
        }

        for (; x < end; x++)
        {
            int fOff = x * 4;
            float sumR = Unsafe.Add(ref ringBase, centerRowOffset + fOff) * centerWeight;
            float sumG = Unsafe.Add(ref ringBase, centerRowOffset + fOff + 1) * centerWeight;
            float sumB = Unsafe.Add(ref ringBase, centerRowOffset + fOff + 2) * centerWeight;

            for (int k = 1; k <= radius; k++)
            {
                int upSlot = centerSlot - k;
                if (upSlot < 0) upSlot += kernelSize;
                int dnSlot = centerSlot + k;
                if (dnSlot >= kernelSize) dnSlot -= kernelSize;

                float w = kernel1D[radius + k];
                int uOff = upSlot * rowFloats + fOff;
                int dOff = dnSlot * rowFloats + fOff;
                sumR += (Unsafe.Add(ref ringBase, uOff) + Unsafe.Add(ref ringBase, dOff)) * w;
                sumG += (Unsafe.Add(ref ringBase, uOff + 1) + Unsafe.Add(ref ringBase, dOff + 1)) * w;
                sumB += (Unsafe.Add(ref ringBase, uOff + 2) + Unsafe.Add(ref ringBase, dOff + 2)) * w;
            }

            Unsafe.Add(ref dstRef, fOff) = (byte)Math.Clamp(sumR, 0.0f, 255.0f);
            Unsafe.Add(ref dstRef, fOff + 1) = (byte)Math.Clamp(sumG, 0.0f, 255.0f);
            Unsafe.Add(ref dstRef, fOff + 2) = (byte)Math.Clamp(sumB, 0.0f, 255.0f);
        }
    }
}
