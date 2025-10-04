using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;

public interface IFilter : IImageProcessor
{
    void Apply(ImageBuffer imageBuffer);
}
