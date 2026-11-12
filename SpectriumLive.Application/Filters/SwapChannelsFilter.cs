using SpectriumLive.Application.Helpers;
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
        uint pattern = selectedMode switch
        {
            SwapMode.RB => 0x03000102u, // byte indices: 2 (B), 1 (G), 0 (R), 3 (A)
            SwapMode.RG => 0x03020001u, // byte indices: 1 (G), 0 (R), 2 (B), 3 (A)
            SwapMode.GB => 0x03010200u, // byte indices: 0 (R), 2 (B), 1 (G), 3 (A)
            _ => 0u
        };

        if (pattern == 0u)
            return;

        PixelSimdHelper.ForEachSlice(imageBuffer.Pixels, pattern, static (slice, state) =>
            PixelSimdHelper.ShuffleChannels(slice, state));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
