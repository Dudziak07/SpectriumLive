using Microsoft.Win32;
using SpectriumLive.Application.Services;
using SpectriumLive.Core;
using SpectriumLive.Wpf.Commands;
using SpectriumLive.Wpf.Converters;
using SpectriumLive.Wpf.ViewModels.Base;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly FilterService filterService;
    public IEnumerable<IFilter> Filters { get; }
    public ICommand LoadImageCommand { get; }

    private ImageSource? originalImage;
    public ImageSource? OriginalImage
    {
        get => originalImage;
        set => SetProperty(ref originalImage, value);
    }

    private ImageSource? processedImage;
    public ImageSource? ProcessedImage
    {
        get => processedImage;
        set => SetProperty(ref processedImage, value);
    }

    private IFilter? selectedFilter;
    public IFilter? SelectedFilter
    {
        get => selectedFilter;
        set
        {
            SetProperty(ref selectedFilter, value);
            ProcessImage();
        }
    }

    private ImageBuffer? originalImageBuffer;

    public MainWindowViewModel(FilterService filterService)
    {
        this.filterService = filterService;
        Filters = this.filterService.GetAvailableFilters();

        LoadImageCommand = new RelayCommand(LoadImage);
    }

    private void LoadImage(object? parameter)
    {
        OpenFileDialog openFileDialog = new()
        {
            Title = "Select an Image",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*"
        };

        if ( openFileDialog.ShowDialog() is true )
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new(openFileDialog.FileName);
            bitmap.EndInit();
            bitmap.Freeze();

            OriginalImage = bitmap;
            originalImageBuffer = ImageConverter.ToImageBuffer(bitmap);

            ProcessImage();
        }
    }

    private void ProcessImage()
    {
        if ( originalImageBuffer is null || SelectedFilter is null )
        {
            ProcessedImage = null;
            return;
        }

        var bufferCopy = new ImageBuffer(originalImageBuffer.Width, originalImageBuffer.Height, (PixelData[])originalImageBuffer.Pixels.Clone());
        SelectedFilter.Apply(bufferCopy);
        ProcessedImage = ImageConverter.ToImageSource(bufferCopy);
    }
}
