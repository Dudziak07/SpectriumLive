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
using SpectriumLive.Wpf.View;
using SpectriumLive.Wpf.ViewModels.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.ViewModels;

public partial class MainWindowViewModel(
    ImageProcessingService imageProcessingService,
    IImageFileService imageFileService,
    CameraService cameraService,
    ThemeService themeService,
    TextRendererService textRendererService) : ViewModelBase
{
    private readonly ImageProcessingService imageProcessingService = imageProcessingService;
    private readonly IImageFileService imageFileService = imageFileService;
    private readonly CameraService cameraService = cameraService;
    private readonly ThemeService themeService = themeService;
    private readonly TextRendererService textRendererService = textRendererService;

    private const int AsciiArtWidth = 150; // Width for ASCII art conversion

    public IEnumerable<ImageProcessorViewModel> ImageProcessors { get; } = [.. imageProcessingService.GetAvailableProcessors()
        .Select(p => new ImageProcessorViewModel(p))];
    public ObservableCollection<CameraDevice> AvailableCameras { get; } = [];

    [ObservableProperty]
    private ImageSource? originalImage;

    [ObservableProperty]
    private ImageSource? processedImage;

    [ObservableProperty]
    private ImageProcessorViewModel? selectedProcessor;

    [ObservableProperty]
    private bool isTextConverterSelected;

    [ObservableProperty]
    private string? asciiResult;

    [ObservableProperty]
    private int selectedCameraIndex = -1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCamerasCommand))]
    private bool isScanningCameras;

    private ImageBuffer? originalImageBuffer;

    private WriteableBitmap? cameraFrameBitmap;

    [RelayCommand]
    private void SelectProcessor(ImageProcessorViewModel? processor)
    {
        if ( ReferenceEquals(SelectedProcessor, processor) )
        {
            SelectedProcessor = null;
        }
        else
        {
            SelectedProcessor = processor;
        }
    }

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
    private void SetLanguage(string cultureCode)
    {
        if ( string.IsNullOrWhiteSpace(cultureCode) )
            return;

        Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);

        var oldWindow = System.Windows.Application.Current.MainWindow;

        var newViewModel = new MainWindowViewModel(
            imageProcessingService,
            imageFileService,
            cameraService,
            themeService,
            textRendererService);
        var newWindow = new MainWindow(newViewModel);
        newWindow.Show();

        System.Windows.Application.Current.MainWindow = newWindow;
        oldWindow?.Close();
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

    private void ProcessImageBasedOnSelection()
    {
        if ( originalImageBuffer is null )
        {
            ProcessedImage = OriginalImage;
            return;
        }

        var processor = SelectedProcessor?.Processor;

        if ( processor is null )
        {
            ProcessedImage = OriginalImage;
            return;
        }

        if ( processor is IFilter filter )
        {
            var bufferCopy = new ImageBuffer(originalImageBuffer.Width, originalImageBuffer.Height, (PixelData[])originalImageBuffer.Pixels.Clone());
            filter.Apply(bufferCopy);
            ProcessedImage = ImageConverter.ToImageSource(bufferCopy);
        }
        else if ( processor is IImageToTextConverter converter )
        {
            string asciiText = converter.Convert(originalImageBuffer);
            ProcessedImage = textRendererService.RenderAsciiArt(asciiText, originalImageBuffer.Width, originalImageBuffer.Height);
        }
    }

    partial void OnSelectedProcessorChanged(ImageProcessorViewModel? oldValue, ImageProcessorViewModel? newValue)
    {
        if ( oldValue?.Processor is INotifyPropertyChanged oldNotifier )
        {
            oldNotifier.PropertyChanged -= OnProcessorPropertyChanged;
        }

        if ( newValue?.Processor is INotifyPropertyChanged newNotifier )
        {
            newNotifier.PropertyChanged += OnProcessorPropertyChanged;
        }

        IsTextConverterSelected = newValue?.Processor is IImageToTextConverter;
        ProcessImageBasedOnSelection();
    }

    private void OnProcessorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ProcessImageBasedOnSelection();
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
        ProcessImageBasedOnSelection();
    }

    partial void OnOriginalImageChanged(ImageSource? value)
    {
        ProcessImageBasedOnSelection();
    }

    partial void OnProcessedImageChanged(ImageSource? value) => SaveImageCommand.NotifyCanExecuteChanged();
}
