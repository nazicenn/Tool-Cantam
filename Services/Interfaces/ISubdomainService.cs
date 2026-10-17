using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ISubdomainService
{
    int MaxCandidatesPerScan { get; }
    int MaxConcurrency { get; }
    int QueryTimeoutMs { get; }

    ToolResult<string> ValidateDomain(string? domain);

    Task<ToolResult<SubdomainScanReport>> ScanAsync(
        string? domain,
        IProgress<SubdomainScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed record SubdomainEntry(string FullName, string IpAddress);

public sealed record SubdomainScanReport(
    string Domain,
    int CandidatesChecked,
    int FoundCount,
    int FailedCount,
    TimeSpan Elapsed,
    IReadOnlyList<SubdomainEntry> Found);

public sealed record SubdomainScanProgress(int Checked, int Total, int Found);