using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using tool_cantam.Core.Enums;

namespace tool_cantam.Helpers.Converters;

/// <summary>
/// Hash tanımlama güven düzeyini tema renk fırçasına çevirir:
/// kesin → başarı, muhtemel → uyarı, olası → ikincil metin rengi.
/// </summary>
public sealed class HashIdentifierConfidenceToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var confidence = value is HashIdentifierConfidence parsed ? parsed : HashIdentifierConfidence.Possible;

        string resourceKey = confidence switch
        {
            HashIdentifierConfidence.Certain => "StatusSuccessBrush",
            HashIdentifierConfidence.Likely => "StatusWarningBrush",
            _ => "TextSecondaryBrush"
        };

        if (Application.Current?.TryFindResource(resourceKey) is Brush themedBrush)
            return themedBrush;

        return confidence switch
        {
            HashIdentifierConfidence.Certain => Brushes.LimeGreen,
            HashIdentifierConfidence.Likely => Brushes.Orange,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
