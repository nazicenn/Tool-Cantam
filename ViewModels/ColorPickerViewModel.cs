// ViewModels/ColorPickerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Media;

namespace tool_cantam.ViewModels;

public partial class ColorPickerViewModel : ObservableObject
{
    [ObservableProperty]
    private Color _selectedColor = Colors.DodgerBlue;

    [ObservableProperty]
    private string _hexColor = "#1E90FF";

    [ObservableProperty]
    private byte _red = 30;

    [ObservableProperty]
    private byte _green = 144;

    [ObservableProperty]
    private byte _blue = 255;

    [ObservableProperty]
    private double _hue;

    [ObservableProperty]
    private double _saturation;

    [ObservableProperty]
    private double _lightness;

    [ObservableProperty]
    private string _statusText = "Ready";

    partial void OnSelectedColorChanged(Color value)
    {
        Red = value.R;
        Green = value.G;
        Blue = value.B;
        HexColor = $"#{value.R:X2}{value.G:X2}{value.B:X2}";

        ColorToHsl(value.R, value.G, value.B, out double h, out double s, out double l);
        Hue = Math.Round(h, 1);
        Saturation = Math.Round(s, 1);
        Lightness = Math.Round(l, 1);

        StatusText = $"Color selected: {HexColor}";
    }

    partial void OnRedChanged(byte value) => UpdateColorFromRgb();
    partial void OnGreenChanged(byte value) => UpdateColorFromRgb();
    partial void OnBlueChanged(byte value) => UpdateColorFromRgb();

    private void UpdateColorFromRgb()
    {
        SelectedColor = Color.FromRgb(Red, Green, Blue);
    }

    private void ColorToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        double red = r / 255.0;
        double green = g / 255.0;
        double blue = b / 255.0;

        double max = Math.Max(red, Math.Max(green, blue));
        double min = Math.Min(red, Math.Min(green, blue));
        double delta = max - min;

        // Hue
        if (delta == 0)
            h = 0;
        else if (max == red)
            h = 60 * (((green - blue) / delta) % 6);
        else if (max == green)
            h = 60 * (((blue - red) / delta) + 2);
        else
            h = 60 * (((red - green) / delta) + 4);

        if (h < 0) h += 360;

        // Lightness
        l = (max + min) / 2 * 100;

        // Saturation
        if (delta == 0)
            s = 0;
        else
            s = delta / (1 - Math.Abs(2 * l / 100 - 1)) * 100;

        h = Math.Round(h, 1);
        s = Math.Round(s, 1);
        l = Math.Round(l, 1);
    }

    [RelayCommand]
    private void CopyHex()
    {
        if (!string.IsNullOrWhiteSpace(HexColor))
        {
            Clipboard.SetText(HexColor);
            StatusText = $"Copied: {HexColor}";
        }
    }

    [RelayCommand]
    private void CopyRgb()
    {
        var rgb = $"rgb({Red}, {Green}, {Blue})";
        Clipboard.SetText(rgb);
        StatusText = $"Copied: {rgb}";
    }

    [RelayCommand]
    private void CopyHsl()
    {
        var hsl = $"hsl({Hue}, {Saturation}%, {Lightness}%)";
        Clipboard.SetText(hsl);
        StatusText = $"Copied: {hsl}";
    }

    [RelayCommand]
    private void PasteHex()
    {
        var text = Clipboard.GetText();
        if (text.StartsWith("#") && (text.Length == 7 || text.Length == 4))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(text);
                SelectedColor = color;
                StatusText = $"Pasted: {text}";
            }
            catch
            {
                StatusText = "Invalid HEX color format";
            }
        }
        else
        {
            StatusText = "Please paste a valid HEX color (e.g., #FF5733)";
        }
    }
}