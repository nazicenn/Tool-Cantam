using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace tool_cantam.Helpers.Converters;

public class PingStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int roundtrip && roundtrip > 0)
            return new SolidColorBrush(Color.FromRgb(0, 255, 65));
        return new SolidColorBrush(Color.FromRgb(255, 49, 49));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}