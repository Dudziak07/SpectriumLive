using SpectriumLive.Core;
using System.Reflection;

namespace SpectriumLive.Application.Services;
public class TextConverterService
{
    public IEnumerable<IImageToTextConverter> GetAvailableConverters()
    {
        var converterTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IImageToTextConverter).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

        foreach ( var type in converterTypes )
        {
            if ( Activator.CreateInstance(type) is IImageToTextConverter converter )
            {
                yield return converter;
            }
        }
    }
}
