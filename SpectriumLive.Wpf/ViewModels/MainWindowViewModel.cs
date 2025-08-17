using SpectriumLive.Application.Contracts;
using SpectriumLive.Application.Helpers;
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
    private readonly TextConverterService textConverterService;
    private readonly IImageFileService imageFileService;

    private const int AsciiArtWidth = 150; // Width for ASCII art conversion

    public IEnumerable<IFilter> Filters { get; }
    public IEnumerable<IImageToTextConverter> TextConverters { get; }
    public ICommand LoadImageCommand { get; }
    public ICommand SaveImageCommand { get; }

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

    private string? asciiResult;
    public string? AsciiResult
    {
        get => asciiResult;
        set => SetProperty(ref asciiResult, value);
    }

    private IImageToTextConverter? selectedTextConverter;
    public IImageToTextConverter? SelectedTextConverter
    {
        get => selectedTextConverter;
        set
        {
            SetProperty(ref selectedTextConverter, value);
            ConvertImageToText();
        }
    }

    private ImageBuffer? originalImageBuffer;

    public MainWindowViewModel(
        FilterService filterService,
        IImageFileService imageFileService,
        TextConverterService textConverterService)
    {
        this.filterService = filterService;
        this.imageFileService = imageFileService;
        this.textConverterService = textConverterService;

        Filters = FilterService.GetAvailableFilters();
        TextConverters = textConverterService.GetAvailableConverters();
        LoadImageCommand = new RelayCommand(LoadImage);
        SaveImageCommand = new RelayCommand(SaveImage, CanSaveImage);
    }

    private void LoadImage(object? parameter)
    {
        var loadedBuffer = imageFileService.LoadImage();

        if ( loadedBuffer is not null )
        {
            originalImageBuffer = loadedBuffer;
            OriginalImage = ImageConverter.ToImageSource(originalImageBuffer);
            ProcessImage();
            ConvertImageToText();
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

    private bool CanSaveImage(object? parameter)
    {
        return ProcessedImage is not null;
    }

    public void SaveImage(object? parameter)
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
}
