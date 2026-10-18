using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICssGradientGeneratorService
{
    Task<ToolResult<GradientReport>> GenerateAsync(GradientOptions options, CancellationToken ct);
}

public enum GradientDirection { ToRight, ToLeft, ToBottom, ToTop, ToBottomRight, ToTopLeft }

public sealed record GradientOptions(string Color1, string Color2, GradientDirection Direction);

public sealed record GradientReport(string Css, string Direction, TimeSpan Elapsed);