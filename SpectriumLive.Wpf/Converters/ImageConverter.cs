using OpenCvSharp;
using SpectriumLive.Core.Entities;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Converters;

public static class ImageConverter
{
    private const int ParallelPixelThreshold = 65_536;

    public static ImageBuffer ToImageBuffer(BitmapSource bitmap)
    {
        var formattedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int width = formattedBitmap.PixelWidth;
        int height = formattedBitmap.PixelHeight;
        int stride = (width * formattedBitmap.Format.BitsPerPixel + 7) / 8;
        int totalBytes = height * stride;
        var pixelBytes = ArrayPool<byte>.Shared.Rent(totalBytes);

        try
        {
            formattedBitmap.CopyPixels(pixelBytes, stride, 0);

            var pixels = new PixelData[width * height];

            Parallel.For(0, height, y =>
            {
                int rowByteOffset = y * stride;
                int rowPixelOffset = y * width;

                for (int x = 0; x < width; x++)
                {
                    int byteIndex = rowByteOffset + x * 4;
                    pixels[rowPixelOffset + x] = new(
                        pixelBytes[byteIndex + 2], // R
                        pixelBytes[byteIndex + 1], // G
                        pixelBytes[byteIndex],     // B
                        pixelBytes[byteIndex + 3]  // A
                    );
                }
            });

            return new ImageBuffer(width, height, pixels);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixelBytes);
        }
    }

    public static void ToImageBuffer(Mat frame, ref ImageBuffer? reusableBuffer)
    {
        int channels = frame.Channels();
        Mat? convertedFrame = null;
        Mat sourceMat = frame;

        if (channels is not (3 or 4))
        {
            convertedFrame = new Mat();
            var conversionCode = channels == 1
                ? ColorConversionCodes.GRAY2BGRA
                : ColorConversionCodes.BGR2BGRA;
            Cv2.CvtColor(frame, convertedFrame, conversionCode);
            sourceMat = convertedFrame;
            channels = 4;
        }

        try
        {
            int width = sourceMat.Width;
            int height = sourceMat.Height;
            int totalPixels = width * height;

            if (reusableBuffer is null ||
                reusableBuffer.Width != width ||
                reusableBuffer.Height != height ||
                reusableBuffer.Pixels.Length != totalPixels)
            {
                reusableBuffer = new ImageBuffer(width, height, new PixelData[totalPixels]);
            }

            if (totalPixels == 0)
                return;

            int rowBytes = width * channels;
            int totalBytes = height * rowBytes;
            byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(totalBytes);

            try
            {
                if (sourceMat.IsContinuous())
                {
                    Marshal.Copy(sourceMat.Data, rentedBytes, 0, totalBytes);
                }
                else
                {
                    nint step = (nint)sourceMat.Step();
                    IntPtr basePtr = sourceMat.Data;
                    for (int y = 0; y < height; y++)
                    {
                        Marshal.Copy(basePtr + (nint)y * step, rentedBytes, y * rowBytes, rowBytes);
                    }
                }

                PixelData[] pixels = reusableBuffer.Pixels;

                if (totalPixels >= ParallelPixelThreshold)
                {
                    int workerCount = Math.Min(Environment.ProcessorCount, totalPixels);
                    int chunkSize = (totalPixels + workerCount - 1) / workerCount;

                    Parallel.For(0, workerCount, worker =>
                    {
                        int start = worker * chunkSize;
                        int end = Math.Min(start + chunkSize, totalPixels);
                        if (start >= end)
                            return;

                        ConvertBgrBytesToPixels(rentedBytes, pixels, start, end, channels);
                    });
                }
                else
                {
                    ConvertBgrBytesToPixels(rentedBytes, pixels, 0, totalPixels, channels);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBytes);
            }
        }
        finally
        {
            convertedFrame?.Dispose();
        }
    }

    public static void WriteToWriteableBitmap(ImageBuffer imageBuffer, ref WriteableBitmap? targetBitmap)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;

        if (targetBitmap is null ||
            targetBitmap.PixelWidth != width ||
            targetBitmap.PixelHeight != height)
        {
            targetBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }

        int totalPixels = width * height;
        if (totalPixels == 0)
            return;

        int stride = width * 4;
        int totalBytes = height * stride;
        byte[] pixelBytes = ArrayPool<byte>.Shared.Rent(totalBytes);

        try
        {
            PixelData[] pixels = imageBuffer.Pixels;

            if (totalPixels >= ParallelPixelThreshold)
            {
                int workerCount = Math.Min(Environment.ProcessorCount, totalPixels);
                int chunkSize = (totalPixels + workerCount - 1) / workerCount;

                Parallel.For(0, workerCount, worker =>
                {
                    int start = worker * chunkSize;
                    int end = Math.Min(start + chunkSize, totalPixels);
                    if (start >= end)
                        return;

                    ConvertPixelsToBgraBytes(pixels, pixelBytes, start, end);
                });
            }
            else
            {
                ConvertPixelsToBgraBytes(pixels, pixelBytes, 0, totalPixels);
            }

            targetBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelBytes, stride, 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixelBytes);
        }
    }

    public static ImageSource ToImageSource(ImageBuffer imageBuffer)
    {
        WriteableBitmap? bitmap = null;
        WriteToWriteableBitmap(imageBuffer, ref bitmap);
        return bitmap!;
    }

    private static void ConvertBgrBytesToPixels(byte[] source, PixelData[] destination, int start, int end, int channels)
    {
        if (channels == 3)
        {
            for (int i = start; i < end; i++)
            {
                int byteIndex = i * 3;
                destination[i] = new PixelData(
                    source[byteIndex + 2],
                    source[byteIndex + 1],
                    source[byteIndex],
                    255);
            }
        }
        else
        {
            for (int i = start; i < end; i++)
            {
                int byteIndex = i * 4;
                destination[i] = new PixelData(
                    source[byteIndex + 2],
                    source[byteIndex + 1],
                    source[byteIndex],
                    source[byteIndex + 3]);
            }
        }
    }

    private static void ConvertPixelsToBgraBytes(PixelData[] source, byte[] destination, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            int byteIndex = i * 4;
            PixelData pixel = source[i];
            destination[byteIndex + 2] = pixel.R;
            destination[byteIndex + 1] = pixel.G;
            destination[byteIndex] = pixel.B;
            destination[byteIndex + 3] = pixel.A;
        }
    }
}
