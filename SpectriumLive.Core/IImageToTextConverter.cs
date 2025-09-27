using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;
public interface IImageToTextConverter : IProcessor
{
    string Convert(ImageBuffer imageBuffer);
}
