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
        var mergedDictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var currentThemeDictionary = mergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));

        if ( currentThemeDictionary == null )
            return;

        var newThemeUri = theme switch
        {
            Theme.Light => new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative),
            _ => new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative),
        };

        currentThemeDictionary!.Source = newThemeUri;
    }
}
