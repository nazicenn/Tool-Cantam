using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>NS lookup — IDnsService'i NS filtresiyle sarmalar.</summary>
public sealed class NsLookupService : INsLookupService
{
    private static readonly TimeSpan TimeoutValue = TimeSpan.FromSeconds(10);
    public TimeSpan Timeout => TimeoutValue;

    private readonly IDnsService _dns;

    public NsLookupService(IDnsService dns) { _dns = dns; }

    public async Task<ToolResult<NsLookupReport>> LookupAsync(string? domain, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<NsLookupReport>.Fail(ToolErrorKind.Validation, NsLookupErrorCodes.EmptyDomain, "Domain is empty.");

        var validation = _dns.ValidateDomain(domain);
        if (!validation.IsSuccess)
            return ToolResult<NsLookupReport>.Fail(ToolErrorKind.Validation, NsLookupErrorCodes.InvalidDomain, "Invalid domain.");

        try
        {
            var sw = Stopwatch.StartNew();
            var result = await _dns.LookupAsync(domain.Trim(), ct);
            sw.Stop();

            if (!result.IsSuccess)
                return ToolResult<NsLookupReport>.Fail(ToolErrorKind.NotFound,
                    NsLookupErrorCodes.DomainNotFound, result.ErrorCode ?? "DNS lookup failed.");

            var ns = result.Value!.Records
                .Where(r => string.Equals(r.Type, "NS", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Value)
                .Distinct()
                .ToList();

            return ToolResult<NsLookupReport>.Ok(new NsLookupReport(domain.Trim(), ns, sw.Elapsed));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<NsLookupReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<NsLookupReport>.Fail(ToolErrorKind.Unexpected, NsLookupErrorCodes.Timeout, "Timeout.");
        }
        catch (Exception ex)
        {
            return ToolResult<NsLookupReport>.Fail(ToolErrorKind.Unexpected, NsLookupErrorCodes.Unexpected, ex.Message);
        }
    }
}