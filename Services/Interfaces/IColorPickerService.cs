using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IColorPickerService
{
    int MaxHexLength { get; }
    Task<ToolResult<ColorPickerReport>> FromHexAsync(string? hex, CancellationToken ct);
    Task<ToolResult<ColorPickerReport>> FromRgbAsync(int r, int g, int b, CancellationToken ct);
    Task<ToolResult<ColorPickerReport>> RandomAsync(CancellationToken ct);
}

public sealed record ColorPickerReport(
    int Red, int Green, int Blue,
    string Hex, string Rgb, string Hsl,
    double Hue, double Saturation, double Lightness,
    bool IsLight,
    TimeSpan Elapsed);