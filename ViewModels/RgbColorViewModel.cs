using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Media;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class RgbColorViewModel : ObservableObject
{
    private readonly IColorConverterService _colorService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private double _red = 0;

    [ObservableProperty]
    private double _green = 0;

    [ObservableProperty]
    private double _blue = 0;

    [ObservableProperty]
    private string _hexValue = "#000000";

    [ObservableProperty]
    private string _hslValue = "hsl(0, 0%, 0%)";

    [ObservableProperty]
    private string _rgbValue = "rgb(0, 0, 0)";

    [ObservableProperty]
    private Color _previewColor = Colors.Black;

    [ObservableProperty]
    private Color _previewTextColor = Colors.White;

    [ObservableProperty]
    private string _colorInfo = "Black color";

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public RgbColorViewModel(
        IColorConverterService colorService,
        IClipboardService clipboardService)
    {
        _colorService = colorService;
        _clipboardService = clipboardService;
    }

    partial void OnRedChanged(double value) => UpdateFromRgb();
    partial void OnGreenChanged(double value) => UpdateFromRgb();
    partial void OnBlueChanged(double value) => UpdateFromRgb();

    private void UpdateFromRgb()
    {
        int r = (int)Math.Clamp(Red, 0, 255);
        int g = (int)Math.Clamp(Green, 0, 255);
        int b = (int)Math.Clamp(Blue, 0, 255);

        HexValue = _colorService.RgbToHex(r, g, b);
        HslValue = _colorService.RgbToHsl(r, g, b);
        RgbValue = $"rgb({r}, {g}, {b})";
        PreviewColor = Color.FromRgb((byte)r, (byte)g, (byte)b);

        // Kontrast için yazı rengi
        double brightness = (r * 0.299 + g * 0.587 + b * 0.114);
        PreviewTextColor = brightness > 128 ? Colors.Black : Colors.White;

        // Renk bilgisi
        ColorInfo = GetColorDescription(r, g, b);
    }

    private string GetColorDescription(int r, int g, int b)
    {
        if (r == 0 && g == 0 && b == 0) return "Black";
        if (r == 255 && g == 255 && b == 255) return "White";
        if (r == 255 && g == 0 && b == 0) return "Pure Red";
        if (r == 0 && g == 255 && b == 0) return "Pure Green";
        if (r == 0 && g == 0 && b == 255) return "Pure Blue";

        string tone = "";
        int avg = (r + g + b) / 3;
        if (avg < 85) tone = "Dark";
        else if (avg < 170) tone = "Medium";
        else tone = "Light";

        string dominant = "";
        int max = Math.Max(r, Math.Max(g, b));
        if (max == r) dominant = "Reddish";
        else if (max == g) dominant = "Greenish";
        else dominant = "Bluish";

        return $"{tone} {dominant} (R:{r}, G:{g}, B:{b})";
    }

    [RelayCommand]
    private void ApplyHex()
    {
        if (_colorService.TryParseHex(HexValue, out int r, out int g, out int b))
        {
            Red = r;
            Green = g;
            Blue = b;
            UpdateStatus($"✓ HEX applied: {HexValue.ToUpperInvariant()}", NotificationType.Success);
        }
        else
        {
            UpdateStatus("✗ Invalid HEX format. Use #RRGGBB or #RGB", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void CopyHex()
    {
        _clipboardService.CopyToClipboard(HexValue);
        UpdateStatus("✓ HEX copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void CopyRgb()
    {
        _clipboardService.CopyToClipboard(RgbValue);
        UpdateStatus("✓ RGB copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void CopyHsl()
    {
        _clipboardService.CopyToClipboard(HslValue);
        UpdateStatus("✓ HSL copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void RandomColor()
    {
        // Her tıklamada new Random() oluşturmak aynı tohumu kullanıp aynı rengi üretebiliyordu.
        Red = Random.Shared.Next(0, 256);
        Green = Random.Shared.Next(0, 256);
        Blue = Random.Shared.Next(0, 256);
        UpdateStatus("🎲 Random color generated", NotificationType.Info);
    }

    [RelayCommand]
    private void Clear()
    {
        Red = 0;
        Green = 0;
        Blue = 0;
        UpdateStatus("✓ Reset to black", NotificationType.Success);
    }

    private void UpdateStatus(string message, NotificationType type)
    {
        StatusText = message;
        StatusType = type;
    }
}