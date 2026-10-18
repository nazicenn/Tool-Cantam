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

/// <summary>HEX ↔ RGB ↔ HSL dönüşümü. CSPRNG ile random.</summary>
public sealed class ColorPickerService : IColorPickerService
{
    public const int MaxHexLengthLimit = 9; // #RRGGBBAA
    public int MaxHexLength => MaxHexLengthLimit;

    private static readonly Regex HexRegex = new(@"^#?(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    public Task<ToolResult<ColorPickerReport>> FromHexAsync(string? hex, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hex) || !HexRegex.IsMatch(hex.Trim()))
            return Fail<ColorPickerReport>(ToolErrorKind.Validation, ColorPickerErrorCodes.InvalidHex, "Invalid HEX.");

        var s = hex.Trim().TrimStart('#');
        if (s.Length == 3) s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";

        try
        {
            int r = int.Parse(s[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return Task.FromResult(ToolResult<ColorPickerReport>.Ok(Build(r, g, b)));
        }
        catch (Exception ex) { return Fail<ColorPickerReport>(ToolErrorKind.Unexpected, ColorPickerErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<ColorPickerReport>> FromRgbAsync(int r, int g, int b, CancellationToken ct)
    {
        if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255)
            return Fail<ColorPickerReport>(ToolErrorKind.Validation, ColorPickerErrorCodes.InvalidRgb, "RGB out of range.");
        return Task.FromResult(ToolResult<ColorPickerReport>.Ok(Build(r, g, b)));
    }

    public Task<ToolResult<ColorPickerReport>> RandomAsync(CancellationToken ct)
    {
        int r = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        int g = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        int b = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 256);
        return Task.FromResult(ToolResult<ColorPickerReport>.Ok(Build(r, g, b)));
    }

    private static ColorPickerReport Build(int r, int g, int b)
    {
        var sw = Stopwatch.StartNew();
        var (h, s, l) = RgbToHsl(r, g, b);
        var hex = $"#{r:X2}{g:X2}{b:X2}";
        var rgb = $"rgb({r}, {g}, {b})";
        var hsl = $"hsl({h:0}, {s:0}%, {l:0}%)";
        bool isLight = (r * 0.299 + g * 0.587 + b * 0.114) > 128;
        sw.Stop();
        return new ColorPickerReport(r, g, b, hex, rgb, hsl, h, s, l, isLight, sw.Elapsed);
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

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}