using System.Globalization;
using System.Windows.Data;

namespace CorraStudio.Presentation.Wpf.Converters;

public class StringToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
            return str == param;
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue && parameter is string param)
            return param;
        return string.Empty;
    }
}
