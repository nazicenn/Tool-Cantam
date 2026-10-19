using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IYamlToJsonService
{
    int MaxInputLength { get; }
    Task<ToolResult<YamlToJsonReport>> ConvertAsync(string? yaml, YamlToJsonOptions options, CancellationToken ct);
}

public sealed record YamlToJsonOptions(bool Indented);
public sealed record YamlToJsonReport(string Json, int InputLength, int OutputLength, TimeSpan Elapsed);