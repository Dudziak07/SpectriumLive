using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using System.ComponentModel;

namespace SpectriumLive.Application.Filters;

public enum ChannelSelector
{
    R,
    G,
    B
}

public class ChannelFilter : IFilter, INotifyPropertyChanged
{
    public string Name => "Channel Filter";
    public ImageProcessorGroup Group => ImageProcessorGroup.Color;
    public bool HasConfigurableProperties => true;

    private ChannelSelector channelSelector = ChannelSelector.R; // Default value
    public ChannelSelector SelectedChannel
    {
        get => channelSelector;
        set
        {
            if ( channelSelector != value )
            {
                channelSelector = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedChannel)));
            }
        }
    }

    public ChannelFilter() { }

    public void Apply(ImageBuffer imageBuffer)
    {
        var pixels = imageBuffer.Pixels;
        switch ( channelSelector )
        {
            case ChannelSelector.R:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(pixel.R, 0, 0, pixel.A);
                });
                break;
            case ChannelSelector.G:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(0, pixel.G, 0, pixel.A);
                });
                break;
            case ChannelSelector.B:
                Parallel.For(0, pixels.Length, i =>
                {
                    var pixel = pixels[i];
                    pixels[i] = new PixelData(0, 0, pixel.B, pixel.A);
                });
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
