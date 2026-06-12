using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CorraStudio.Presentation.Wpf.Converters;

[ValueConversion(typeof(double), typeof(Thickness))]
public class ThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double thickness)
        {
            return new Thickness(thickness);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Thickness thickness)
        {
            return thickness.Left;
        }
        return 0d;
    }
}
