using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace tool_cantam.Helpers.Converters;

/// <summary>
/// false değerini görünür, true değerini gizli yapar. İki denetimin aynı alanda yer değiştirmesi
/// (ör. parola alanı ↔ düz metin alanı) için kullanılır.
/// </summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool flag && flag ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Visibility visibility && visibility != Visibility.Visible;
}
