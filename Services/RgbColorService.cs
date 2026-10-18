using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>RGB ↔ HEX ↔ HSL dönüşümü. Bağımsız, yan etkisiz.</summary>
public sealed class RgbColorService : IRgbColorService
{
    private static readonly Regex HexRegex = new(@"^#?[0-9A-Fa-f]{3}(?:[0-9A-Fa-f]{3})?$", RegexOptions.Compiled);

    public Task<ToolResult<ColorReport>> FromRgbAsync(int r, int g, int b, CancellationToken ct)
    {
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            return Fail<ColorReport>(ToolErrorKind.Validation, RgbColorErrorCodes.InvalidRgb, "RGB must be 0..255.");

        var sw = Stopwatch.StartNew();
        var report = Build(r, g, b);
        sw.Stop();
        return Task.FromResult(ToolResult<ColorReport>.Ok(report));
    }

    public Task<ToolResult<ColorReport>> FromHexAsync(string? hex, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hex) || !HexRegex.IsMatch(hex.Trim()))
            return Fail<ColorReport>(ToolErrorKind.Validation, RgbColorErrorCodes.InvalidHex, "Invalid HEX.");

        var s = hex.Trim().TrimStart('#');
        if (s.Length == 3) s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";

        int r = int.Parse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int g = int.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int b = int.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return Task.FromResult(ToolResult<ColorReport>.Ok(Build(r, g, b)));
    }

    public Task<ToolResult<ColorReport>> FromHslAsync(double h, double s, double l, CancellationToken ct)
    {
        if (h < 0 || h > 360 || s < 0 || s > 100 || l < 0 || l > 100)
            return Fail<ColorReport>(ToolErrorKind.Validation, RgbColorErrorCodes.InvalidHsl, "HSL out of range.");

        double sn = s / 100.0, ln = l / 100.0;
        double c = (1 - Math.Abs(2 * ln - 1)) * sn;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = ln - c / 2;

        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        int r = (int)Math.Round((r1 + m) * 255);
        int g = (int)Math.Round((g1 + m) * 255);
        int b = (int)Math.Round((b1 + m) * 255);

        return Task.FromResult(ToolResult<ColorReport>.Ok(Build(r, g, b)));
    }

    public Task<ToolResult<ColorReport>> RandomAsync(CancellationToken ct)
    {
        int r = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        int g = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        int b = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        return Task.FromResult(ToolResult<ColorReport>.Ok(Build(r, g, b)));
    }

    private static ColorReport Build(int r, int g, int b)
    {
        var (h, s, l) = RgbToHsl(r, g, b);
        var hex = $"#{r:X2}{g:X2}{b:X2}";
        var rgb = $"rgb({r}, {g}, {b})";
        var hsl = $"hsl({h:0}, {s:0}%, {l:0}%)";
        var desc = Describe(r, g, b);
        bool isLight = (r * 0.299 + g * 0.587 + b * 0.114) > 128;
        return new ColorReport(r, g, b, hex, rgb, hsl, desc, h, s, l, isLight);
    }

    private static (double H, double S, double L) RgbToHsl(int r, int g, int b)
    {
        double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;
        double max = Math.Max(rn, Math.Max(gn, bn));
        double min = Math.Min(rn, Math.Min(gn, bn));
        double l = (max + min) / 2;
        double h = 0, s = 0;

        if (max != min)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == rn) h = ((gn - bn) / d + (gn < bn ? 6 : 0)) * 60;
            else if (max == gn) h = ((bn - rn) / d + 2) * 60;
            else h = ((rn - gn) / d + 4) * 60;
        }

        return (h, s * 100, l * 100);
    }

    private static string Describe(int r, int g, int b)
    {
        if (r == 0 && g == 0 && b == 0) return "Black";
        if (r == 255 && g == 255 && b == 255) return "White";
        if (r == 255 && g == 0 && b == 0) return "Pure Red";
        if (r == 0 && g == 255 && b == 0) return "Pure Green";
        if (r == 0 && g == 0 && b == 255) return "Pure Blue";

        int avg = (r + g + b) / 3;
        string tone = avg < 85 ? "Dark" : avg < 170 ? "Medium" : "Light";
        int max = Math.Max(r, Math.Max(g, b));
        string dom = max == r ? "Reddish" : max == g ? "Greenish" : "Bluish";
        return $"{tone} {dom} (R:{r}, G:{g}, B:{b})";
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}