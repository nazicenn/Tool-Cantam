using System;
using System.Globalization;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class ColorConverterService : IColorConverterService
{
    public string RgbToHex(int r, int g, int b)
    {
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public string RgbToHsl(int r, int g, int b)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        double h = 0;
        double s = 0;
        double l = (max + min) / 2.0;

        if (delta != 0)
        {
            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (max == rd)
                h = ((gd - bd) / delta) + (gd < bd ? 6 : 0);
            else if (max == gd)
                h = ((bd - rd) / delta) + 2;
            else
                h = ((rd - gd) / delta) + 4;

            h *= 60;
        }

        return $"hsl({(int)Math.Round(h)}, {(int)Math.Round(s * 100)}%, {(int)Math.Round(l * 100)}%)";
    }

    public (int R, int G, int B) HexToRgb(string hex)
    {
        if (!TryParseHex(hex, out int r, out int g, out int b))
            throw new FormatException($"Invalid HEX color: {hex}");
        return (r, g, b);
    }

    public (int R, int G, int B) HslToRgb(double h, double s, double l)
    {
        s /= 100.0;
        l /= 100.0;

        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = l - c / 2;

        double rd = 0, gd = 0, bd = 0;

        if (h < 60) { rd = c; gd = x; bd = 0; }
        else if (h < 120) { rd = x; gd = c; bd = 0; }
        else if (h < 180) { rd = 0; gd = c; bd = x; }
        else if (h < 240) { rd = 0; gd = x; bd = c; }
        else if (h < 300) { rd = x; gd = 0; bd = c; }
        else { rd = c; gd = 0; bd = x; }

        return (
            (int)Math.Round((rd + m) * 255),
            (int)Math.Round((gd + m) * 255),
            (int)Math.Round((bd + m) * 255)
        );
    }

    public bool TryParseHex(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        hex = hex.Trim();

        // Yalnızca tek bir baştaki '#' kabul edilir ("##123" geçerli sayılmaz).
        if (hex.StartsWith('#'))
            hex = hex[1..];

        if (hex.Length == 3)
        {
            hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
        }

        if (hex.Length != 6) return false;

        foreach (char c in hex)
        {
            bool isHexDigit = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHexDigit) return false;
        }

        r = int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        g = int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        b = int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return true;
    }
}