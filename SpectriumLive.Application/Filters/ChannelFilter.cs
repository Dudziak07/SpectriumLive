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
        Parallel.For(0, imageBuffer.Pixels.Length, i =>
        {
            var pixel = imageBuffer.Pixels[i];

            imageBuffer.Pixels[i] = channelSelector switch
            {
                ChannelSelector.R => new PixelData(pixel.R, 0, 0, pixel.A),
                ChannelSelector.G => new PixelData(0, pixel.G, 0, pixel.A),
                ChannelSelector.B => new PixelData(0, 0, pixel.B, pixel.A),
                _ => pixel
            };
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
