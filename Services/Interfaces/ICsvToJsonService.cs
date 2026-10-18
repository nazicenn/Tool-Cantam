using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICsvToJsonService
{
    int MaxInputLength { get; }
    Task<ToolResult<CsvReport>> ConvertAsync(string? input, CsvOptions options, CancellationToken ct);
}

public sealed record CsvOptions(
    char Delimiter,
    bool FirstRowAsHeader,
    bool TrimValues,
    bool DetectTypes,
    int IndentSize);

public sealed record CsvReport(string OutputJson, int Rows, int Columns, TimeSpan Elapsed);