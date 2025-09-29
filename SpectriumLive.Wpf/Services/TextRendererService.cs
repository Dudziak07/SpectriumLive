using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpectriumLive.Wpf.Services;
public class TextRendererService
{
    public ImageSource RenderTextToBitmap(string text, string fontFamily, double fontSize, Brush foreground, Brush background)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily),
            fontSize,
            foreground,
            1.25);

        var drawingVisual = new DrawingVisual();
        using ( var drawingContext = drawingVisual.RenderOpen() )
        {
            drawingContext.DrawRectangle(background, null, new Rect(0, 0, formattedText.Width, formattedText.Height));
            drawingContext.DrawText(formattedText, new Point(2, 2));
        }

        var bitmap = new RenderTargetBitmap(
            (int)( formattedText.WidthIncludingTrailingWhitespace + 4 ),
            (int)( formattedText.Height + 4 ),
            96,
            96,
            PixelFormats.Pbgra32);

        bitmap.Render(drawingVisual);
        return bitmap;
    }

    public ImageSource RenderAsciiArt(string asciiText, int originalWidth, int originalHeight)
    {
        if ( string.IsNullOrEmpty(asciiText) )
            return null!;

        var lines = asciiText.Split([Environment.NewLine], StringSplitOptions.None);
        int textWidth = lines.Max(l => l.Length);
        int textHeight = lines.Length;

        if ( textWidth == 0 || textHeight == 0 )
            return null!;

        double fontAspectRatio = 0.45;
        double imageAspectRatio = (double)originalHeight / originalWidth;
        double fontSize = (double)originalWidth / textWidth * imageAspectRatio / fontAspectRatio;

        if ( fontSize < 1 )
            fontSize = 1;

        var foreground = (Brush)System.Windows.Application.Current.Resources["PrimaryForegroundColor"];
        var background = (Brush)System.Windows.Application.Current.Resources["PrimaryBackgroundColor"];

        return RenderTextToBitmap(asciiText, "Consolas", fontSize, foreground, background);
    }
}
