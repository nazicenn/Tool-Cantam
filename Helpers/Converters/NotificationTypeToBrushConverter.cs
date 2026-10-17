using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Helpers.Converters;

/// <summary>
/// Durum türünü tema renk fırçasına çevirir. Renkler tema sözlüğünden gelir;
/// tema anahtarı bulunamazsa okunabilir bir yedek renk kullanılır.
/// </summary>
public sealed class NotificationTypeToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var type = value is NotificationType notificationType ? notificationType : NotificationType.Info;

        string resourceKey = type switch
        {
            NotificationType.Success => "StatusSuccessBrush",
            NotificationType.Warning => "StatusWarningBrush",
            NotificationType.Error => "StatusDangerBrush",
            _ => "TextSecondaryBrush"
        };

        if (Application.Current?.TryFindResource(resourceKey) is Brush themedBrush)
            return themedBrush;

        return type switch
        {
            NotificationType.Success => Brushes.LimeGreen,
            NotificationType.Warning => Brushes.Orange,
            NotificationType.Error => Brushes.IndianRed,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
