using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Subdomain tarayıcısının tek uygulaması.
/// </summary>
public sealed class SubdomainService : ISubdomainService
{
    public const int MaxCandidatesLimit = 5000;
    public const int MaxConcurrencyLimit = 50;
    public const int QueryTimeoutMsLimit = 3000;

    private const int ProgressBatchSize = 10;

    public int MaxCandidatesPerScan => MaxCandidatesLimit;
    public int MaxConcurrency => MaxConcurrencyLimit;
    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    private static readonly string[] CandidateNames =
    {
        "www", "mail", "ftp", "webmail", "smtp", "pop", "pop3", "imap", "ns", "ns1", "ns2", "ns3",
        "cpanel", "whm", "autodiscover", "autoconfig", "m", "test", "blog", "dev", "www2", "admin",
        "forum", "news", "vpn", "mail2", "new", "mysql", "old", "lists", "support", "mobile",
        "mx", "static", "docs", "beta", "shop", "sql", "secure", "demo", "cp", "calendar", "wiki",
        "web", "media", "email", "images", "img", "download", "dns", "piwik", "stats", "dashboard",
        "portal", "manage", "start", "info", "app", "store", "api", "gateway", "oauth", "login",
        "auth", "sso", "id", "account", "cdn", "assets", "files", "cloud", "s3", "git", "repo",
        "stage", "staging", "prod", "production", "backup", "db", "database", "redis", "kafka",
        "monitor", "metrics", "logs", "traefik", "nginx", "apache", "proxy", "edge", "mobile-api",
        "internal", "intranet", "extranet", "vpn2", "remote", "gitlab", "jenkins", "ci", "cd"
    };

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, "Domain is empty.");

        string trimmed = domain.Trim().ToLowerInvariant();

        if (trimmed.Length > 253)
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, $"Domain is too long ({trimmed.Length} characters).");

        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains(':'))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, $"Domain contains invalid characters: '{trimmed}'.");

        if (trimmed.Contains('_'))
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, $"Domain contains underscore: '{trimmed}'.");

        if (Uri.CheckHostName(trimmed) == UriHostNameType.Unknown)
            return ToolResult<string>.Fail(ToolErrorKind.Validation, SubdomainErrorCodes.InvalidDomain, $"'{trimmed}' is not a valid domain.");

        return ToolResult<string>.Ok(trimmed);
    }

    public async Task<ToolResult<SubdomainScanReport>> ScanAsync(
        string? domain,
        IProgress<SubdomainScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDomain(domain);
        if (!validation.IsSuccess)
            return ToolResult<SubdomainScanReport>.Fail(validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string targetDomain = validation.Value!;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Dns.GetHostAddressesAsync(targetDomain, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return Cancelled(); }
        catch (SocketException ex)
        {
            return ToolResult<SubdomainScanReport>.Fail(ToolErrorKind.NotFound, SubdomainErrorCodes.DomainNotFound, ex.Message);
        }

        var found = new List<SubdomainEntry>();
        int checkedCount = 0, foundCount = 0, failedCount = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxConcurrencyLimit,
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(CandidateNames, options, async (sub, token) =>
            {
                string full = $"{sub}.{targetDomain}";
                string? ip = await TryResolveAsync(full, token).ConfigureAwait(false);

                if (ip is not null)
                {
                    lock (found) found.Add(new SubdomainEntry(full, ip));
                    Interlocked.Increment(ref foundCount);
                }
                else
                {
                    Interlocked.Increment(ref failedCount);
                }

                int done = Interlocked.Increment(ref checkedCount);

                if (progress is not null && (done % ProgressBatchSize == 0 || done == CandidateNames.Length))
                    progress.Report(new SubdomainScanProgress(done, CandidateNames.Length, Volatile.Read(ref foundCount)));
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return Cancelled(); }
        catch (Exception ex)
        {
            return ToolResult<SubdomainScanReport>.Fail(ToolErrorKind.Unexpected, SubdomainErrorCodes.Unexpected, ex.ToString());
        }

        stopwatch.Stop();
        found.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.OrdinalIgnoreCase));

        return ToolResult<SubdomainScanReport>.Ok(new SubdomainScanReport(
            targetDomain, checkedCount, foundCount, failedCount, stopwatch.Elapsed, found));
    }

    private static async Task<string?> TryResolveAsync(string host, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(QueryTimeoutMsLimit);

        try
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, timeoutSource.Token).ConfigureAwait(false);
            foreach (IPAddress addr in addresses)
                if (addr.AddressFamily == AddressFamily.InterNetwork) return addr.ToString();
            return addresses.Length > 0 ? addresses[0].ToString() : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return null; }
    }

    private static ToolResult<SubdomainScanReport> Cancelled() =>
        ToolResult<SubdomainScanReport>.Fail(ToolErrorKind.Cancelled, SubdomainErrorCodes.Cancelled, "Scan cancelled by the user.");
}