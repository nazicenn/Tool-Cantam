using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using tool_cantam.Core.Enums;

namespace tool_cantam.Helpers.Converters;

/// <summary>
/// Hash karşılaştırma durumunu tema renk fırçasına çevirir. Renkler tema sözlüğünden gelir;
/// tema anahtarı bulunamazsa okunabilir bir yedek renk kullanılır.
/// </summary>
public sealed class HashMatchStateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value is HashMatchState matchState ? matchState : HashMatchState.NotChecked;

        string resourceKey = state switch
        {
            HashMatchState.Matched => "StatusSuccessBrush",
            HashMatchState.Mismatched => "StatusDangerBrush",
            HashMatchState.NotComparable => "StatusWarningBrush",
            _ => "TextSecondaryBrush"
        };

        if (Application.Current?.TryFindResource(resourceKey) is Brush themedBrush)
            return themedBrush;

        return state switch
        {
            HashMatchState.Matched => Brushes.LimeGreen,
            HashMatchState.Mismatched => Brushes.IndianRed,
            HashMatchState.NotComparable => Brushes.Orange,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
