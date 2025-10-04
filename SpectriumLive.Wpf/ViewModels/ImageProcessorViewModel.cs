using SpectriumLive.Core;
using SpectriumLive.Core.Entities;
using SpectriumLive.Wpf.Resources.Strings;

namespace SpectriumLive.Wpf.ViewModels;
public class ImageProcessorViewModel
{
    public IImageProcessor Processor { get; }
    public string Name => Processor.Name;
    public string GroupName { get; }

    public ImageProcessorViewModel(IImageProcessor imageProcessor)
    {
        Processor = imageProcessor;
        GroupName = GetLocalizedGroupName(imageProcessor.Group);
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
