using SpectriumLive.Core;
using System.Reflection;

namespace SpectriumLive.Application.Services;

public class FilterService
{
    public static IEnumerable<IFilter> GetAvailableFilters()
    {
        var filterTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IFilter).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

        foreach ( var type in filterTypes )
        {
            if ( Activator.CreateInstance(type) is IFilter filter )
            {
                yield return filter;
            }
        }
    }
}
