using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IRgbColorService
{
    Task<ToolResult<ColorReport>> FromRgbAsync(int r, int g, int b, CancellationToken ct);
    Task<ToolResult<ColorReport>> FromHexAsync(string? hex, CancellationToken ct);
    Task<ToolResult<ColorReport>> FromHslAsync(double h, double s, double l, CancellationToken ct);
    Task<ToolResult<ColorReport>> RandomAsync(CancellationToken ct);
}

public sealed record ColorReport(
    int Red, int Green, int Blue,
    string Hex, string Rgb, string Hsl,
    string Description,
    double Hue, double Saturation, double Lightness,
    bool IsLight);