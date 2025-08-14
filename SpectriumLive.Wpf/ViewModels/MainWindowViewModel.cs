using Microsoft.Win32;
using SpectriumLive.Application.Services;
using SpectriumLive.Core;
using SpectriumLive.Wpf.Commands;
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

            OriginalImage = bitmap;

            ProcessedImage = null;
        }
    }
}
