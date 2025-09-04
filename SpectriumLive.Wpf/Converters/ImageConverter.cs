using SpectriumLive.Core.Entities;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Converters;

public static class ImageConverter
{
    public static ImageBuffer ToImageBuffer(BitmapSource bitmap)
    {
        var formattedBitmap = new FormatConvertedBitmap(
            bitmap,
            PixelFormats.Bgra32,
            null,
            0);
        var stride = ( formattedBitmap.PixelWidth * formattedBitmap.Format.BitsPerPixel + 7 ) / 8;
        var pixelBytes = new byte[formattedBitmap.PixelHeight * stride];
        formattedBitmap.CopyPixels(pixelBytes, stride, 0);

        var pixels = new PixelData[formattedBitmap.PixelWidth * formattedBitmap.PixelHeight];
        for ( int y = 0; y < formattedBitmap.PixelHeight; y++ )
        {
            for ( int x = 0; x < formattedBitmap.PixelWidth; x++ )
            {
                int pixelIndex = y * formattedBitmap.PixelWidth + x;
                int byteIndex = y * stride + x * 4;
                pixels[pixelIndex] = new(
                    pixelBytes[byteIndex + 2], // R
                    pixelBytes[byteIndex + 1], // G
                    pixelBytes[byteIndex + 0], // B
                    pixelBytes[byteIndex + 3]  // A
                );
            }
        }

        return new ImageBuffer(formattedBitmap.PixelWidth, formattedBitmap.PixelHeight, pixels);
    }

    public static ImageSource ToImageSource(ImageBuffer imageBuffer)
    {
        var format = PixelFormats.Bgra32;
        var stride = imageBuffer.Width * ( format.BitsPerPixel / 8 );
        var pixelBytes = new byte[imageBuffer.Height * stride];

        for ( int i = 0; i < imageBuffer.Pixels.Length; i++ )
        {
            int byteIndex = i * 4;
            pixelBytes[byteIndex + 2] = imageBuffer.Pixels[i].R; // R
            pixelBytes[byteIndex + 1] = imageBuffer.Pixels[i].G; // G
            pixelBytes[byteIndex + 0] = imageBuffer.Pixels[i].B; // B
            pixelBytes[byteIndex + 3] = imageBuffer.Pixels[i].A; // A
        }

        var bitmap = new WriteableBitmap(
            imageBuffer.Width,
            imageBuffer.Height,
            96, // DPI X
            96, // DPI Y
            format,
            null);
        bitmap.WritePixels(new(
            0, 0, imageBuffer.Width, imageBuffer.Height),
            pixelBytes,
            stride,
            0);

        return bitmap;
    }
}
