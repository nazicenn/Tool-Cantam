using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ISqlFormatterService
{
    int MaxInputLength { get; }
    Task<ToolResult<SqlReport>> FormatAsync(string? input, SqlOptions options, CancellationToken ct);
    Task<ToolResult<SqlReport>> MinifyAsync(string? input, CancellationToken ct);
    Task<ToolResult<SqlValidation>> ValidateAsync(string? input, CancellationToken ct);
}

public sealed record SqlOptions(
    bool UppercaseKeywords,
    int IndentSize,
    bool NewlineBeforeAndOr,
    bool CommaNewline);

public sealed record SqlReport(string Output, int OriginalLength, int OutputLength, TimeSpan Elapsed);
public sealed record SqlValidation(bool IsValid, string? Error, int StatementCount);