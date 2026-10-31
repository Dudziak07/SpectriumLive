using System.Globalization;
using System.Windows.Data;

namespace SpectriumLive.Wpf.Converters;

public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => Equals(value, parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? parameter ?? Binding.DoNothing : Binding.DoNothing;
}
