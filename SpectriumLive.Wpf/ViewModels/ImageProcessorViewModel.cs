using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using SpectriumLive.Wpf.Resources.Strings;

namespace SpectriumLive.Wpf.ViewModels;
public class ImageProcessorViewModel
{
    public IImageProcessor Processor { get; }
    public string Name { get; }
    public string GroupName { get; }

    public ImageProcessorViewModel(IImageProcessor imageProcessor)
    {
        Processor = imageProcessor;
        Name = GetLocalizedName(imageProcessor.Name);
        GroupName = GetLocalizedGroupName(imageProcessor.Group);
    }

    private string GetLocalizedName(string processorName)
    {
        return processorName switch
        {
            "ASCII" => AppResources.Filter_Ascii,
            "Binary" => AppResources.Filter_Binary,
            "Grayscale" => AppResources.Filter_Grayscale,
            "Negative" => AppResources.Filter_Negative,
            "Sepia" => AppResources.Filter_Sepia,
            "Swap Channels" => AppResources.Filter_SwapChannels,
            "Channel Filter" => AppResources.Filter_ChannelFilter,
            "Box Blur" => AppResources.Filter_BoxBlur,
            "Gaussian Blur" => AppResources.Filter_GaussianBlur,
            "Edge Detection" => AppResources.Filter_EdgeDetection,
            "Fisheye" => AppResources.Filter_Fisheye,
            _ => processorName
        };
    }

    private string GetLocalizedGroupName(ImageProcessorGroup group)
    {
        return group switch
        {
            ImageProcessorGroup.Ascii => AppResources.Group_Ascii,
            ImageProcessorGroup.Color => AppResources.Group_Color,
            ImageProcessorGroup.Convolution => AppResources.Group_Convolution,
            ImageProcessorGroup.Distortion => AppResources.Group_Distortion,
            _ => group.ToString()
        };
    }
}
