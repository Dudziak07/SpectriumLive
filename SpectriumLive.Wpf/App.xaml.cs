using Microsoft.Extensions.DependencyInjection;
using SpectriumLive.Application.Contracts;
using SpectriumLive.Application.Services;
using SpectriumLive.Wpf.Services;
using SpectriumLive.Wpf.View;
using SpectriumLive.Wpf.ViewModels;
using System.Windows;

namespace SpectriumLive.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly ServiceProvider serviceProvider;

    public App()
    {
        ServiceCollection serviceCollection = new();
        ConfigureServices(serviceCollection);
        serviceProvider = serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register your services here
        services.AddSingleton<FilterService>();
        services.AddSingleton<MainWindowViewModel>();

        // Add other services as needed
        services.AddSingleton<MainWindow>();

        services.AddSingleton<IImageFileService, WpfImageFileService>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }
}

