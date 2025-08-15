using Microsoft.Win32;
using SpectriumLive.Application.Contracts;
using SpectriumLive.Core;
using SpectriumLive.Wpf.Converters;
using System.IO;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Services;
public class WpfImageFileService : IImageFileService
{
    public ImageBuffer? LoadImage()
    {
        OpenFileDialog openFileDialog = new()
        {
            Title = "Select an Image",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*"
        };

        if ( openFileDialog.ShowDialog() is true )
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new(openFileDialog.FileName);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return ImageConverter.ToImageBuffer(bitmapImage);
        }

        return null;
    }

    public void SaveImage(ImageBuffer imageBuffer)
    {
        SaveFileDialog saveFileDialog = new()
        {
            Title = "Save Image",
            Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp",
            DefaultExt = "png",
            FileName = "processed_image.png"
        };

        if ( saveFileDialog.ShowDialog() is true )
        {
            var imageSource = ImageConverter.ToImageSource(imageBuffer);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource as BitmapSource));

            using var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create);
            encoder.Save(fileStream);
        }
    }
}
