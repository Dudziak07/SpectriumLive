using System.Globalization;
using System.Windows.Data;

namespace SpectriumLive.Wpf.Converters;
public class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values is [var a, var b, ..] && ReferenceEquals(a, b);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
