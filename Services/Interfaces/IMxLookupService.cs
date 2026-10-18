using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IMxLookupService
{
    TimeSpan Timeout { get; }
    Task<ToolResult<MxLookupReport>> LookupAsync(string? domain, CancellationToken ct);
}

public sealed record MxRecord(int Priority, string Exchange);
public sealed record MxLookupReport(string Domain, IReadOnlyList<MxRecord> Records, TimeSpan Elapsed);