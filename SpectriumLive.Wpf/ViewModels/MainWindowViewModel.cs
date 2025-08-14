using SpectriumLive.Application.Services;
using SpectriumLive.Core;
using System.Windows.Input;

namespace SpectriumLive.Wpf.ViewModels;

public class MainWindowViewModel
{
    private readonly FilterService filterService;
    public IEnumerable<IFilter> Filters { get; }

    public ICommand LoadImageCommand { get; }

    public MainWindowViewModel(FilterService filterService)
    {
        this.filterService = filterService;
        Filters = this.filterService.GetAvailableFilters();

        LoadImageCommand = new RelayCommand(LoadImage);
    }

    private void LoadImage(object? parameter)
    {
    }
}
