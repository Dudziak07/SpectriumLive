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

        string themeFileName = theme switch
        {
            Theme.Light => "LightTheme.xaml",
            _ => "DarkTheme.xaml",
        };

        var newThemeUri = new Uri($"pack://application:,,,/SpectriumLive.Styles;component/Themes/{themeFileName}", UriKind.Absolute);

        currentThemeDictionary!.Source = newThemeUri;
    }
}
