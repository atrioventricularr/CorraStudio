using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CorraStudio.Presentation.Wpf.Converters;

public class ErrorColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasError && hasError)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
