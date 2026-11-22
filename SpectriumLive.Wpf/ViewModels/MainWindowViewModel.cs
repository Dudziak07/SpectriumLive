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
    [NotifyCanExecuteChangedFor(nameof(ClearImageCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(ToggleCameraCommand))]
    private int selectedCameraIndex = -1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCamerasCommand))]
    private bool isScanningCameras;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FreezeCameraCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearImageCommand))]
    private bool isCameraRunning;

    [ObservableProperty]
    private bool isCameraFrozen;

    private ImageBuffer? originalImageBuffer;
    private ImageBuffer? cameraWorkingBuffer;

    private WriteableBitmap? cameraFrameBitmap;
    private WriteableBitmap? processedFrameBitmap;

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
            EnsureWorkingBuffer(originalImageBuffer.Width, originalImageBuffer.Height);
            int totalPixels = originalImageBuffer.Width * originalImageBuffer.Height;
            Array.Copy(originalImageBuffer.Pixels, cameraWorkingBuffer!.Pixels, totalPixels);
            filter.Apply(cameraWorkingBuffer);
            ImageConverter.WriteToWriteableBitmap(cameraWorkingBuffer, ref processedFrameBitmap);
            if ( !ReferenceEquals(ProcessedImage, processedFrameBitmap) )
            {
                ProcessedImage = processedFrameBitmap;
            }
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

    private void StartCamera()
    {
        OriginalImage = null;
        ProcessedImage = null;
        cameraFrameBitmap = null;
        processedFrameBitmap = null;
        cameraWorkingBuffer = null;
        cameraService.StartCamera(SelectedCameraIndex, OnNewCameraFrame);
    }

    private void StopCamera()
    {
        cameraService.StopCamera();
        OriginalImage = null;
        ProcessedImage = null;
        cameraFrameBitmap = null;
        processedFrameBitmap = null;
        cameraWorkingBuffer = null;
    }

    [RelayCommand(CanExecute = nameof(CanToggleCamera))]
    private void ToggleCamera()
    {
        if ( IsCameraRunning )
        {
            StopCamera();
            IsCameraRunning = false;
        }
        else
        {
            StartCamera();
            IsCameraRunning = true;
        }

        IsCameraFrozen = false;
    }

    private bool CanToggleCamera() => SelectedCameraIndex >= 0 || IsCameraRunning;

    [RelayCommand(CanExecute = nameof(CanFreezeCamera))]
    private void FreezeCamera()
    {
        IsCameraFrozen = !IsCameraFrozen;
    }

    private bool CanFreezeCamera() => IsCameraRunning;

    private void OnNewCameraFrame(Mat frame)
    {
        if ( IsCameraFrozen )
            return;

        ImageConverter.ToImageBuffer(frame, ref originalImageBuffer);
        ImageBuffer sourceBuffer = originalImageBuffer!;

        var processor = SelectedProcessor?.Processor;
        string? asciiText = null;

        if ( processor is IFilter filter )
        {
            EnsureWorkingBuffer(sourceBuffer.Width, sourceBuffer.Height);
            int totalPixels = sourceBuffer.Width * sourceBuffer.Height;
            Array.Copy(sourceBuffer.Pixels, cameraWorkingBuffer!.Pixels, totalPixels);
            filter.Apply(cameraWorkingBuffer);
        }
        else if ( processor is IImageToTextConverter converter )
        {
            asciiText = converter.Convert(sourceBuffer);
        }

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            if ( !IsCameraRunning || IsCameraFrozen )
                return;

            if ( cameraFrameBitmap is null ||
                cameraFrameBitmap.PixelWidth != frame.Width ||
                cameraFrameBitmap.PixelHeight != frame.Height )
            {
                cameraFrameBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr24, null);
                OriginalImage = cameraFrameBitmap;
            }

            WriteableBitmapConverter.ToWriteableBitmap(frame, cameraFrameBitmap);

            if ( processor is IFilter && cameraWorkingBuffer is not null )
            {
                ImageConverter.WriteToWriteableBitmap(cameraWorkingBuffer, ref processedFrameBitmap);
                if ( !ReferenceEquals(ProcessedImage, processedFrameBitmap) )
                {
                    ProcessedImage = processedFrameBitmap;
                }
            }
            else if ( processor is IImageToTextConverter && asciiText is not null )
            {
                ProcessedImage = textRendererService.RenderAsciiArt(asciiText, sourceBuffer.Width, sourceBuffer.Height);
            }
            else if ( processor is null )
            {
                if ( !ReferenceEquals(ProcessedImage, OriginalImage) )
                {
                    ProcessedImage = OriginalImage;
                }
            }
        });
    }

    private void EnsureWorkingBuffer(int width, int height)
    {
        int totalPixels = width * height;
        if ( cameraWorkingBuffer is null ||
            cameraWorkingBuffer.Width != width ||
            cameraWorkingBuffer.Height != height ||
            cameraWorkingBuffer.Pixels.Length != totalPixels )
        {
            cameraWorkingBuffer = new ImageBuffer(width, height, new PixelData[totalPixels]);
        }
    }

    [RelayCommand(CanExecute = nameof(CanClearImage))]
    private void ClearImage()
    {
        if ( IsCameraRunning )
        {
            if ( ToggleCameraCommand.CanExecute(null) )
                ToggleCameraCommand.Execute(null);
        }
        else
        {
            originalImageBuffer = null;
            cameraWorkingBuffer = null;
            processedFrameBitmap = null;
            OriginalImage = null;
        }
    }

    private bool CanClearImage() => IsCameraRunning || OriginalImage is not null;

    partial void OnOriginalImageChanged(ImageSource? value) => ProcessImageBasedOnSelection();

    partial void OnProcessedImageChanged(ImageSource? value) => SaveImageCommand.NotifyCanExecuteChanged();
}
