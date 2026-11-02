using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters;

public enum SwapMode
{
    RB,
    RG,
    GB
}

public class SwapChannelsFilter : IFilter, INotifyPropertyChanged
{
    public string Name => "Swap Channels";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => true;

    private SwapMode selectedMode = SwapMode.RB; // Default mode
    public SwapMode SelectedMode
    {
        get => selectedMode;
        set
        {
            if ( selectedMode != value )
            {
                selectedMode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMode)));
            }
        }
    }

    public SwapChannelsFilter() { }

    public void Apply(ImageBuffer imageBuffer)
    {
        var pixels = imageBuffer.Pixels;
        switch ( selectedMode )
        {
            case SwapMode.RB:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(pixel.B, pixel.G, pixel.R, pixel.A);
                });
                break;
            case SwapMode.RG:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(pixel.G, pixel.R, pixel.B, pixel.A);
                });
                break;
            case SwapMode.GB:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(pixel.R, pixel.B, pixel.G, pixel.A);
                });
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
