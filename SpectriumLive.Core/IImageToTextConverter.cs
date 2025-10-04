using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;
public interface IImageToTextConverter : IImageProcessor
{
    string Convert(ImageBuffer imageBuffer);
    string Convert(ImageBuffer imageBuffer, int blockSize);
}
