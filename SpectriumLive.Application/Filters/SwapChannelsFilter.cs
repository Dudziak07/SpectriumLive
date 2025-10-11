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
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            imageBuffer.Pixels[i] = selectedMode switch
            {
                SwapMode.RB => new PixelData(pixel.B, pixel.G, pixel.R, pixel.A),
                SwapMode.RG => new PixelData(pixel.G, pixel.R, pixel.B, pixel.A),
                SwapMode.GB => new PixelData(pixel.R, pixel.B, pixel.G, pixel.A),
                _ => pixel
            };
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
