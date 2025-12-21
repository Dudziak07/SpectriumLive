using SpectriumLive.Core.Entities;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Converters;

public static class ImageConverter
{
    public static ImageBuffer ToImageBuffer(BitmapSource bitmap)
    {
        var formattedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int width = formattedBitmap.PixelWidth;
        int height = formattedBitmap.PixelHeight;
        int stride = (width * formattedBitmap.Format.BitsPerPixel + 7) / 8;
        var pixelBytes = new byte[height * stride];
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

    public static ImageSource ToImageSource(ImageBuffer imageBuffer)
    {
        int width = imageBuffer.Width;
        int height = imageBuffer.Height;
        var format = PixelFormats.Bgra32;
        int stride = width * 4;
        var pixelBytes = new byte[height * stride];
        var pixels = imageBuffer.Pixels;

        Parallel.For(0, height, y =>
        {
            int rowByteOffset = y * stride;
            int rowPixelOffset = y * width;

            for (int x = 0; x < width; x++)
            {
                int byteIndex = rowByteOffset + x * 4;
                var pixel = pixels[rowPixelOffset + x];
                pixelBytes[byteIndex + 2] = pixel.R;
                pixelBytes[byteIndex + 1] = pixel.G;
                pixelBytes[byteIndex] = pixel.B;
                pixelBytes[byteIndex + 3] = pixel.A;
            }
        });

        var bitmap = new WriteableBitmap(width, height, 96, 96, format, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelBytes, stride, 0);

        return bitmap;
    }
}
