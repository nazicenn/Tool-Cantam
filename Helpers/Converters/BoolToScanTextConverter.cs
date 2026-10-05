using System;
using System.Globalization;
using System.Windows.Data;

namespace tool_cantam.Helpers.Converters;

public class BoolToScanTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isScanning && isScanning)
            return "SCANNING...";
        return "START SCAN";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return false;
    }
}