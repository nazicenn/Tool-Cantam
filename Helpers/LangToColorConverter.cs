using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace tool_cantam.Helpers;

public sealed class LangToColorConverter : IValueConverter
{
    // Renkleri bir kez oluşturup donduruyoruz (Performance Best Practice)
    private static readonly SolidColorBrush SelectedBrush = CreateFrozenBrush("#0066CC");
    private static readonly SolidColorBrush DefaultBrush = CreateFrozenBrush("#1A1A1A");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Gelen değerleri güvenli bir şekilde karşılaştırıyoruz
        if (value == null || parameter == null)
            return DefaultBrush;

        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
            ? SelectedBrush
            : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing; // NotImplementedException yerine DoNothing daha kurumsaldır

    private static SolidColorBrush CreateFrozenBrush(string hexCode)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexCode));
        brush.Freeze(); // WPF'in bu fırçayı değiştirmeyeceğini bilmesini sağlar, hızı artırır
        return brush;
    }
}