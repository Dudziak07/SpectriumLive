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
}
