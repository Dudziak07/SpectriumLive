using SpectriumLive.Application.Helpers;
using SpectriumLive.Application.TextConverters;
using SpectriumLive.Core;

namespace SpectriumLive.Application.Services;
public class TextConverterService
{
    public IEnumerable<IImageToTextConverter> GetAvailableConverters()
    {
        yield return new AsciiConverter("ASCII (Binary)", AsciiCharacterRamps.Binary);
        yield return new AsciiConverter("ASCII (Simple)", AsciiCharacterRamps.Simple);
        yield return new AsciiConverter("ASCII (Full)", AsciiCharacterRamps.Full);
    }
}
