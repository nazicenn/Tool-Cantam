using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IDnsService
{
    int QueryTimeoutMs { get; }
    IReadOnlyList<string> SupportedRecordTypes { get; }

    ToolResult<string> ValidateDomain(string? domain);

    Task<ToolResult<DnsLookupReport>> LookupAsync(
        string? domain,
        CancellationToken cancellationToken = default);
}

public sealed record DnsRecordEntry(string Type, string Name, string Value, int? Priority = null);

public sealed record DnsLookupReport(
    string Domain,
    TimeSpan Elapsed,
    IReadOnlyList<DnsRecordEntry> Records)
{
    public int TotalCount => Records.Count;
    public int CountByType(string type)
    {
        int count = 0;
        foreach (var r in Records)
            if (string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase)) count++;
        return count;
    }
}