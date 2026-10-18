using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IJsonMinifierService
{
    int MaxInputLength { get; }
    Task<ToolResult<JsonMinReport>> MinifyAsync(string? input, CancellationToken ct);
    Task<ToolResult<JsonMinReport>> FormatAsync(string? input, int indentSize, CancellationToken ct);
}

public sealed record JsonMinReport(string Output, int OriginalLength, int OutputLength, int NodeCount, TimeSpan Elapsed);