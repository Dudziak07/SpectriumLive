using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;
public interface IImageToTextConverter
{
    string Name { get; }
    string Convert(ImageBuffer imageBuffer);
}
