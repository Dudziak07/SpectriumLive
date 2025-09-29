using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SpectriumLive.Application.Contracts;
using SpectriumLive.Application.Services;
using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using SpectriumLive.Wpf.Converters;
using SpectriumLive.Wpf.Services;
using SpectriumLive.Wpf.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.ViewModels;

public partial class MainWindowViewModel(
    FilterService filterService,
    IImageFileService imageFileService,
    TextConverterService textConverterService,
    CameraService cameraService,
    ThemeService themeService,
    TextRendererService textRendererService) : ViewModelBase
{
    private readonly FilterService filterService = filterService;
    private readonly TextConverterService textConverterService = textConverterService;
    private readonly IImageFileService imageFileService = imageFileService;
    private readonly CameraService cameraService = cameraService;
    private readonly ThemeService themeService = themeService;
    private readonly TextRendererService textRendererService = textRendererService;

    private const int AsciiArtWidth = 150; // Width for ASCII art conversion
    public string AsciiBlockSizeText => $"Block Size: {AsciiBlockSize:F0}";

    public IEnumerable<IFilter> Filters { get; } = filterService.GetAvailableFilters();
    public IEnumerable<IImageToTextConverter> TextConverters { get; } = textConverterService.GetAvailableConverters();
    public ObservableCollection<CameraDevice> AvailableCameras { get; } = [];

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

    [ObservableProperty]
    private int selectedCameraIndex = -1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCamerasCommand))]
    private bool isScanningCameras;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AsciiBlockSizeText))]
    private double asciiBlockSize = 8;

    private ImageBuffer? originalImageBuffer;

    private WriteableBitmap? cameraFrameBitmap;

    [RelayCommand]
    private void SetTheme(string themeName)
    {
        var theme = themeName.ToLower() switch
        {
            "light" => Theme.Light,
            _ => Theme.Dark
        };

        themeService.SwitchTheme(theme);
    }

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
            if ( SelectedFilter is null )
                ProcessedImage = OriginalImage;

            return;
        }

        string asciiText = SelectedTextConverter.Convert(originalImageBuffer, (int)AsciiBlockSize);
        ProcessedImage = textRendererService.RenderAsciiArt(asciiText, originalImageBuffer.Width, originalImageBuffer.Height);
    }

    [RelayCommand(CanExecute = nameof(CanLoadCameras))]
    private async Task LoadCamerasAsync()
    {
        IsScanningCameras = true;
        AvailableCameras.Clear();

        try
        {
            var availableCameras = await cameraService.GetAvailableCamerasAsync();
            foreach ( var camera in availableCameras )
            {
                AvailableCameras.Add(camera);
            }
        }
        finally
        {
            IsScanningCameras = false;
        }
    }

    private bool CanLoadCameras() => !IsScanningCameras;

    [RelayCommand]
    private void StartCamera()
    {
        if ( SelectedCameraIndex >= 0 )
        {
            OriginalImage = null;
            ProcessedImage = null;
            cameraFrameBitmap = null;
            cameraService.StartCamera(SelectedCameraIndex, OnNewCameraFrame);
        }
    }

    [RelayCommand]
    private void StopCamera()
    {
        cameraService.StopCamera();
    }

    private void OnNewCameraFrame(Mat frame)
    {
        if ( cameraFrameBitmap is null ||
            cameraFrameBitmap.PixelWidth != frame.Width ||
            cameraFrameBitmap.PixelHeight != frame.Height )
        {
            cameraFrameBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);
            OriginalImage = cameraFrameBitmap;
        }
        WriteableBitmapConverter.ToWriteableBitmap(frame, cameraFrameBitmap);

        originalImageBuffer = ImageConverter.ToImageBuffer(cameraFrameBitmap);
        ProcessImage();
        ConvertImageToText();
    }

    partial void OnOriginalImageChanged(ImageSource? value)
    {
        ProcessImage();
        ConvertImageToText();
    }

    partial void OnSelectedFilterChanged(IFilter? value)
    {
        if ( value is not null )
        {
            selectedTextConverter = null;
            OnPropertyChanged(nameof(SelectedTextConverter));
        }

        ProcessImage();
    }

    partial void OnSelectedTextConverterChanged(IImageToTextConverter? value)
    {
        if ( value is not null )
        {
            selectedFilter = null;
            OnPropertyChanged(nameof(SelectedFilter));
        }

        ConvertImageToText();
    }

    partial void OnProcessedImageChanged(ImageSource? value) => SaveImageCommand.NotifyCanExecuteChanged();
    partial void OnAsciiBlockSizeChanged(double value) => ConvertImageToText();
}
