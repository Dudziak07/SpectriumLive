using SpectriumLive.Core.Entities;

namespace SpectriumLive.Core;
public interface IImageProcessor
{
    string Name { get; }
    ImageProcessorGroup Group { get; }
}
