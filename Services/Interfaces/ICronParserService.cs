using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICronParserService
{
    Task<ToolResult<CronReport>> ParseAsync(string? expr, CancellationToken ct);
}

public sealed record CronField(string Name, string Raw, string Description, int Min, int Max);
public sealed record CronReport(
    string Expression,
    IReadOnlyList<CronField> Fields,
    string Summary,
    IReadOnlyList<DateTime> NextRuns,
    TimeSpan Elapsed);