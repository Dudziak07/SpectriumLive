using SpectriumLive.Application.Services;
using SpectriumLive.Core;

namespace SpectriumLive.Wpf.ViewModels;

public class MainWindowViewModel
{
    private readonly FilterService filterService;
    public IEnumerable<IFilter> Filters { get; }

    public MainWindowViewModel(FilterService filterService)
    {
        this.filterService = filterService;
        Filters = this.filterService.GetAvailableFilters();
    }
}
