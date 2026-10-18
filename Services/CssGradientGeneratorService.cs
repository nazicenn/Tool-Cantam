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

/// <summary>CSS linear-gradient üretir. #RRGGBB veya #RGB kabul eder.</summary>
public sealed class CssGradientGeneratorService : ICssGradientGeneratorService
{
    private static readonly Regex HexRegex = new(@"^#?[0-9A-Fa-f]{3,8}$", RegexOptions.Compiled);

    public Task<ToolResult<GradientReport>> GenerateAsync(GradientOptions o, CancellationToken ct)
    {
        if (o is null)
            return Fail<GradientReport>(ToolErrorKind.Validation, CssGradientGeneratorErrorCodes.InvalidColor, "Options are null.");
        if (!IsValidHex(o.Color1) || !IsValidHex(o.Color2))
            return Fail<GradientReport>(ToolErrorKind.Validation, CssGradientGeneratorErrorCodes.InvalidColor,
                "Colors must be #RRGGBB or #RGB.");
        if (string.Equals(NormalizeHex(o.Color1), NormalizeHex(o.Color2), StringComparison.OrdinalIgnoreCase))
            return Fail<GradientReport>(ToolErrorKind.Validation, CssGradientGeneratorErrorCodes.SameColors,
                "Colors must differ.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var dir = MapDirection(o.Direction);
            var css = $"background: linear-gradient({dir}, {NormalizeHex(o.Color1)}, {NormalizeHex(o.Color2)});";
            sw.Stop();
            return Task.FromResult(ToolResult<GradientReport>.Ok(new GradientReport(css, dir, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<GradientReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<GradientReport>(ToolErrorKind.Unexpected, CssGradientGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static bool IsValidHex(string s) => !string.IsNullOrWhiteSpace(s) && HexRegex.IsMatch(s);

    private static string NormalizeHex(string s) => s.StartsWith('#') ? s.ToUpperInvariant() : $"#{s.ToUpperInvariant()}";

    private static string MapDirection(GradientDirection d) => d switch
    {
        GradientDirection.ToLeft => "to left",
        GradientDirection.ToBottom => "to bottom",
        GradientDirection.ToTop => "to top",
        GradientDirection.ToBottomRight => "to bottom right",
        GradientDirection.ToTopLeft => "to top left",
        _ => "to right",
    };

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}