using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace tool_cantam.Helpers;

/// <summary>
/// String değerini Visibility'ye dönüştürür.
/// Boş veya null string -> Collapsed, dolu string -> Visible.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    // XAML içinde statik olarak erişim sağlar: {Binding Path=Error, Converter={x:Static helpers:StringToVisibilityConverter.Instance}}
    public static readonly StringToVisibilityConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Sadece null/empty değil, sadece boşluk karakterleri (space) içerenleri de yakalayalım
        bool isVisible = !string.IsNullOrWhiteSpace(value as string);

        // Eğer ConverterParameter="Inverse" verilmişse mantığı tersine çevir
        if (parameter as string == "Inverse")
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing; // Geri dönüş desteklenmediği için en güvenli yol budur
    }
}   