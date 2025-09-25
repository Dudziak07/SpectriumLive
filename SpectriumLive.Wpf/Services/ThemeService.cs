using System.Windows;

namespace SpectriumLive.Wpf.Services;

public enum Theme
{
    Dark,
    Light
}

public class ThemeService
{
    public void SwitchTheme(Theme theme)
    {
        var themeDictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        themeDictionaries.Clear();

        var newThemeUri = theme switch
        {
            Theme.Light => new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative),
            _ => new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative),
        };

        themeDictionaries.Add(new ResourceDictionary { Source = newThemeUri });
    }
}
