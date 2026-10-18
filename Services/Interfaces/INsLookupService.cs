using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface INsLookupService
{
    TimeSpan Timeout { get; }
    Task<ToolResult<NsLookupReport>> LookupAsync(string? domain, CancellationToken ct);
}

public sealed record NsLookupReport(string Domain, IReadOnlyList<string> NameServers, TimeSpan Elapsed);