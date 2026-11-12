using SpectriumLive.Application.Helpers;
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
        uint mask = channelSelector switch
        {
            ChannelSelector.R => 0xFF0000FFu,
            ChannelSelector.G => 0xFF00FF00u,
            ChannelSelector.B => 0xFFFF0000u,
            _ => 0u
        };

        if (mask == 0u)
            return;

        PixelSimdHelper.ForEachSlice(imageBuffer.Pixels, mask, static (slice, state) =>
            PixelSimdHelper.ApplyAndMask(slice, state));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
