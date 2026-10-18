using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IYamlFormatterService
{
    int MaxInputLength { get; }
    Task<ToolResult<YamlReport>> FormatAsync(string? input, int indentSize, CancellationToken ct);
    Task<ToolResult<YamlReport>> MinifyAsync(string? input, CancellationToken ct);
}

public sealed record YamlReport(string Output, int OriginalLength, int OutputLength, TimeSpan Elapsed);