using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;

public interface IFilter : IProcessor
{
    void Apply(ImageBuffer imageBuffer);
}
