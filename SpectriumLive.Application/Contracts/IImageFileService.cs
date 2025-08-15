using SpectriumLive.Core;

namespace SpectriumLive.Application.Contracts;
public interface IImageFileService
{
    ImageBuffer? LoadImage();
    void SaveImage(ImageBuffer imageBuffer);
}
