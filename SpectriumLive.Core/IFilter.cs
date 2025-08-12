namespace SpectriumLive.Core;

public interface IFilter
{
    string Name { get; }
    void Apply(ImageBuffer imageBuffer);
}
