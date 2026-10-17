using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// DNS sorgusunun tek uygulaması. A, AAAA, MX, NS, TXT ve SOA kayıtlarını toplar.
/// </summary>
public sealed class DnsService : IDnsService
{
    public const int QueryTimeoutMsLimit = 5000;

    private static readonly string[] RecordTypes = { "A", "AAAA", "MX", "NS", "TXT", "SOA" };

    private readonly LookupClient _client;

    public DnsService()
    {
        _client = new LookupClient(new LookupClientOptions
        {
            Timeout = TimeSpan.FromMilliseconds(QueryTimeoutMsLimit),
            Retries = 1,
            UseCache = false,
            ThrowDnsErrors = false
        });
    }

    public int QueryTimeoutMs => QueryTimeoutMsLimit;
    public IReadOnlyList<string> SupportedRecordTypes => RecordTypes;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, "Domain is empty.");

        string trimmed = domain.Trim().ToLowerInvariant();

        if (trimmed.Length > 253)
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, $"Domain is too long ({trimmed.Length} characters).");

        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains(':'))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, $"Domain contains invalid characters: '{trimmed}'.");

        if (trimmed.Contains('_'))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, $"Domain contains underscore: '{trimmed}'.");

        if (Uri.CheckHostName(trimmed) == UriHostNameType.Unknown)
            return ToolResult<string>.Fail(ToolErrorKind.Validation, DnsErrorCodes.InvalidDomain, $"'{trimmed}' is not a valid domain.");

        return ToolResult<string>.Ok(trimmed);
    }

    public async Task<ToolResult<DnsLookupReport>> LookupAsync(
        string? domain,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDomain(domain);
        if (!validation.IsSuccess)
            return ToolResult<DnsLookupReport>.Fail(validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string target = validation.Value!;
        var stopwatch = Stopwatch.StartNew();
        var records = new List<DnsRecordEntry>();

        try
        {
            await CollectAddressRecordsAsync(target, records, cancellationToken).ConfigureAwait(false);
            await CollectMxAsync(target, records, cancellationToken).ConfigureAwait(false);
            await CollectNsAsync(target, records, cancellationToken).ConfigureAwait(false);
            await CollectTxtAsync(target, records, cancellationToken).ConfigureAwait(false);
            await CollectSoaAsync(target, records, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return Cancelled(); }
        catch (Exception ex)
        {
            return ToolResult<DnsLookupReport>.Fail(ToolErrorKind.Unexpected, DnsErrorCodes.Unexpected, ex.ToString());
        }

        stopwatch.Stop();

        if (records.Count == 0)
            return ToolResult<DnsLookupReport>.Fail(ToolErrorKind.NotFound, DnsErrorCodes.DomainNotFound, $"No DNS records found for '{target}'.");

        records.Sort((a, b) =>
        {
            int cmp = string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase);
        });

        return ToolResult<DnsLookupReport>.Ok(new DnsLookupReport(target, stopwatch.Elapsed, records));
    }

    private async Task CollectAddressRecordsAsync(string domain, List<DnsRecordEntry> records, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var hostEntry = await _client.GetHostEntryAsync(domain).ConfigureAwait(false);
            if (hostEntry is null) return;

            foreach (var ip in hostEntry.AddressList)
            {
                string type = ip.AddressFamily == AddressFamily.InterNetwork ? "A" : "AAAA";
                records.Add(new DnsRecordEntry(type, domain, ip.ToString()));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task CollectMxAsync(string domain, List<DnsRecordEntry> records, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var response = await _client.QueryAsync(domain, QueryType.MX, QueryClass.IN).ConfigureAwait(false);
            if (response.HasError) return;
            foreach (var mx in response.Answers.MxRecords())
                records.Add(new DnsRecordEntry("MX", domain, mx.Exchange.Value.TrimEnd('.'), mx.Preference));
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task CollectNsAsync(string domain, List<DnsRecordEntry> records, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var response = await _client.QueryAsync(domain, QueryType.NS, QueryClass.IN).ConfigureAwait(false);
            if (response.HasError) return;
            foreach (var ns in response.Answers.NsRecords())
            {
                string name = ns.NSDName.Value.TrimEnd('.');
                if (name.Length > 0) records.Add(new DnsRecordEntry("NS", domain, name));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task CollectTxtAsync(string domain, List<DnsRecordEntry> records, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var response = await _client.QueryAsync(domain, QueryType.TXT, QueryClass.IN).ConfigureAwait(false);
            if (response.HasError) return;
            foreach (var txt in response.Answers.TxtRecords())
            {
                string value = string.Join("", txt.EscapedText);
                if (value.Length > 0) records.Add(new DnsRecordEntry("TXT", domain, value));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task CollectSoaAsync(string domain, List<DnsRecordEntry> records, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var response = await _client.QueryAsync(domain, QueryType.SOA, QueryClass.IN).ConfigureAwait(false);
            if (response.HasError) return;
            foreach (var soa in response.Answers.SoaRecords())
            {
                string value = $"{soa.MName.Value.TrimEnd('.')} {soa.RName.Value.TrimEnd('.')} serial={soa.Serial} refresh={soa.Refresh} retry={soa.Retry} expire={soa.Expire} minimum={soa.Minimum}";
                records.Add(new DnsRecordEntry("SOA", domain, value));
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private static ToolResult<DnsLookupReport> Cancelled() =>
        ToolResult<DnsLookupReport>.Fail(ToolErrorKind.Cancelled, DnsErrorCodes.Cancelled, "DNS lookup cancelled by the user.");
}