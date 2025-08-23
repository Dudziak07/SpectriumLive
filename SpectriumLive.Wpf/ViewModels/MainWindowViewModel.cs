using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpectriumLive.Application.Contracts;
using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.Services;
using SpectriumLive.Core;
using SpectriumLive.Wpf.Converters;
using SpectriumLive.Wpf.ViewModels.Base;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.ViewModels;

public partial class MainWindowViewModel(
    FilterService filterService,
    IImageFileService imageFileService,
    TextConverterService textConverterService) : ViewModelBase
{
    private readonly FilterService filterService = filterService;
    private readonly TextConverterService textConverterService = textConverterService;
    private readonly IImageFileService imageFileService = imageFileService;

    private const int AsciiArtWidth = 150; // Width for ASCII art conversion

    public IEnumerable<IFilter> Filters { get; } = FilterService.GetAvailableFilters();
    public IEnumerable<IImageToTextConverter> TextConverters { get; } = textConverterService.GetAvailableConverters();

    [ObservableProperty]
    private ImageSource? originalImage;

    [ObservableProperty]
    private ImageSource? processedImage;

    [ObservableProperty]
    private IFilter? selectedFilter;

    [ObservableProperty]
    private string? asciiResult;

    [ObservableProperty]
    private IImageToTextConverter? selectedTextConverter;

    private ImageBuffer? originalImageBuffer;

    [RelayCommand]
    private void LoadImage()
    {
        var loadedBuffer = imageFileService.LoadImage();

        if ( loadedBuffer is not null )
        {
            originalImageBuffer = loadedBuffer;
            OriginalImage = ImageConverter.ToImageSource(originalImageBuffer);
        }
    }

    private void ProcessImage()
    {
        if ( originalImageBuffer is null || SelectedFilter is null )
        {
            ProcessedImage = OriginalImage;
            return;
        }

        var bufferCopy = new ImageBuffer(originalImageBuffer.Width, originalImageBuffer.Height, (PixelData[])originalImageBuffer.Pixels.Clone());
        SelectedFilter.Apply(bufferCopy);
        ProcessedImage = ImageConverter.ToImageSource(bufferCopy);
    }

    private bool CanSaveImage() => ProcessedImage is not null;

    [RelayCommand(CanExecute = nameof(CanSaveImage))]
    public void SaveImage()
    {
        if ( ProcessedImage is BitmapSource processedBitmap )
        {
            var processedBuffer = ImageConverter.ToImageBuffer(processedBitmap);
            imageFileService.SaveImage(processedBuffer);
        }
    }

    private void ConvertImageToText()
    {
        if ( originalImageBuffer is null || SelectedTextConverter is null )
        {
            AsciiResult = string.Empty;
            return;
        }
        var resizedBuffer = ImageResizer.Resize(originalImageBuffer, AsciiArtWidth);
        AsciiResult = SelectedTextConverter.Convert(resizedBuffer);
    }

    partial void OnOriginalImageChanged(ImageSource? value)
    {
        ProcessImage();
        ConvertImageToText();
    }

    partial void OnSelectedFilterChanged(IFilter? value) => ProcessImage();
    partial void OnSelectedTextConverterChanged(IImageToTextConverter? value) => ConvertImageToText();
}
