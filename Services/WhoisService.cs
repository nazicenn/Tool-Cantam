using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// WHOIS sorgusunun tek uygulaması. TLD'ye göre doğru WHOIS sunucusunu seçer,
/// TCP port 43 üzerinden sorgular ve yanıtı ayrıştırır.
/// <para>
/// Kurallar: bağlantı ve okuma zaman aşımına sahiptir, kullanıcı iptali alt katmana iletilir,
/// yanıt boyutu sınırlıdır ve ayrıştırma eksik alanlara karşı dayanıklıdır.
/// </para>
/// </summary>
public sealed class WhoisService : IWhoisService
{
    public const int QueryTimeoutMsLimit = 10000;
    private const int MaxResponseBytes = 512 * 1024;   // 512 KB üst sınır
    private const int ReadBufferSize = 8192;

    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    public ToolResult<string> ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                WhoisErrorCodes.InvalidDomain,
                "Domain is empty.");
        }

        string trimmed = domain.Trim().ToLowerInvariant();

        if (trimmed.Length > 253)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                WhoisErrorCodes.InvalidDomain,
                $"Domain is too long ({trimmed.Length} characters).");
        }

        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains(':'))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                WhoisErrorCodes.InvalidDomain,
                $"Domain contains invalid characters: '{trimmed}'.");
        }

        if (Uri.CheckHostName(trimmed) != UriHostNameType.Dns)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                WhoisErrorCodes.InvalidDomain,
                $"'{trimmed}' is not a valid DNS host name.");
        }

        return ToolResult<string>.Ok(trimmed);
    }

    public async Task<ToolResult<WhoisReport>> LookupAsync(
        string? domain,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateDomain(domain);
        if (!validation.IsSuccess)
            return ToolResult<WhoisReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string targetDomain = validation.Value!;
        string tld = ExtractTld(targetDomain);
        string? whoisServer = GetWhoisServer(tld);

        if (whoisServer is null)
        {
            return ToolResult<WhoisReport>.Fail(
                ToolErrorKind.Validation,
                WhoisErrorCodes.UnsupportedTld,
                $"No WHOIS server known for TLD '{tld}'.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            string rawText = await QueryWhoisAsync(whoisServer, targetDomain, cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return ToolResult<WhoisReport>.Fail(
                    ToolErrorKind.NotFound,
                    WhoisErrorCodes.NoData,
                    "WHOIS server returned an empty response.");
            }

            // "No match" / "not found" tespiti
            if (LooksLikeNotFound(rawText))
            {
                return ToolResult<WhoisReport>.Fail(
                    ToolErrorKind.NotFound,
                    WhoisErrorCodes.NoData,
                    $"No WHOIS record found for '{targetDomain}'.");
            }

            var report = ParseReport(targetDomain, tld, whoisServer, rawText, stopwatch.Elapsed);

            return ToolResult<WhoisReport>.Ok(report);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return ToolResult<WhoisReport>.Fail(
                ToolErrorKind.Unexpected,
                WhoisErrorCodes.QueryTimeout,
                $"WHOIS query timed out after {QueryTimeoutMsLimit} ms.");
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();
            return ToolResult<WhoisReport>.Fail(
                ToolErrorKind.Unexpected,
                WhoisErrorCodes.ServerUnreachable,
                ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<WhoisReport>.Fail(
                ToolErrorKind.Unexpected,
                WhoisErrorCodes.Unexpected,
                ex.ToString());
        }
    }

    private static async Task<string> QueryWhoisAsync(
        string whoisServer, string domain, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(QueryTimeoutMsLimit);

        using var client = new TcpClient();
        await client.ConnectAsync(whoisServer, 43, timeoutSource.Token).ConfigureAwait(false);

        await using var stream = client.GetStream();

        byte[] request = Encoding.ASCII.GetBytes($"{domain}\r\n");
        await stream.WriteAsync(request, timeoutSource.Token).ConfigureAwait(false);

        var response = new StringBuilder();
        byte[] buffer = new byte[ReadBufferSize];
        int totalRead = 0;

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, timeoutSource.Token).ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            totalRead += bytesRead;
            if (totalRead > MaxResponseBytes)
            {
                throw new InvalidOperationException(
                    $"WHOIS response exceeded {MaxResponseBytes} bytes.");
            }

            response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
        }

        return response.ToString();
    }

    private static bool LooksLikeNotFound(string rawText)
    {
        // Farklı WHOIS sunucuları farklı mesajlar döner; en yaygın olanlar.
        string lower = rawText.ToLowerInvariant();
        return lower.Contains("no match for")
            || lower.Contains("not found")
            || lower.Contains("no entries found")
            || lower.Contains("no data found")
            || lower.Contains("no matching record")
            || lower.Contains("domain not found");
    }

    private static WhoisReport ParseReport(
        string domain, string tld, string whoisServer, string rawText, TimeSpan elapsed)
    {
        string? registrar = null;
        string? creation = null;
        string? expiration = null;
        string? updated = null;
        string? registrantName = null;
        string? registrantOrg = null;
        string? registrantCountry = null;
        string? registrantEmail = null;

        var nameServers = new List<string>();
        var statuses = new List<string>();

        foreach (string rawLine in rawText.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0 || line.StartsWith('%') || line.StartsWith('#'))
                continue;

            int colon = line.IndexOf(':');
            if (colon <= 0)
                continue;

            string key = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim();

            if (value.Length == 0)
                continue;

            if (MatchesKey(key, "Registrar", "Sponsoring Registrar", "Registrar Name"))
                registrar ??= value;
            else if (MatchesKey(key, "Creation Date", "Created", "Created On", "Registration Time"))
                creation ??= value;
            else if (MatchesKey(key, "Registry Expiry Date", "Registrar Registration Expiration Date",
                                "Expiration Date", "Expiry Date", "Expiration Time"))
                expiration ??= value;
            else if (MatchesKey(key, "Updated Date", "Last Updated On", "Last Modified"))
                updated ??= value;
            else if (MatchesKey(key, "Name Server", "Nameserver", "Name Servers"))
                AddUnique(nameServers, value.ToLowerInvariant());
            else if (MatchesKey(key, "Domain Status", "Status"))
                AddUnique(statuses, value);
            else if (MatchesKey(key, "Registrant Name"))
                registrantName ??= value;
            else if (MatchesKey(key, "Registrant Organization", "Registrant Organisation"))
                registrantOrg ??= value;
            else if (MatchesKey(key, "Registrant Country"))
                registrantCountry ??= value;
            else if (MatchesKey(key, "Registrant Email"))
                registrantEmail ??= value;
        }

        return new WhoisReport(
            domain,
            tld,
            whoisServer,
            registrar ?? string.Empty,
            creation ?? string.Empty,
            expiration ?? string.Empty,
            updated ?? string.Empty,
            nameServers,
            statuses,
            registrantName ?? string.Empty,
            registrantOrg ?? string.Empty,
            registrantCountry ?? string.Empty,
            registrantEmail ?? string.Empty,
            elapsed,
            rawText);
    }

    private static bool MatchesKey(string key, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.Equals(key, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void AddUnique(List<string> list, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        foreach (var existing in list)
        {
            if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))
                return;
        }
        list.Add(value);
    }

    private static string ExtractTld(string domain)
    {
        int lastDot = domain.LastIndexOf('.');
        return lastDot > 0 ? domain[(lastDot + 1)..] : domain;
    }

    private static string? GetWhoisServer(string tld) => tld switch
    {
        "com" or "net" => "whois.verisign-grs.com",
        "org" => "whois.pir.org",
        "edu" => "whois.educause.edu",
        "gov" => "whois.nic.gov",
        "info" or "biz" => "whois.afilias.net",
        "io" => "whois.nic.io",
        "co" => "whois.nic.co",
        "ai" => "whois.nic.ai",
        "app" or "dev" or "page" => "whois.nic.google",
        "xyz" => "whois.nic.xyz",
        "tr" => "whois.nic.tr",
        "uk" => "whois.nic.uk",
        "de" => "whois.denic.de",
        "fr" => "whois.nic.fr",
        "jp" => "whois.jprs.jp",
        "ru" => "whois.tcinet.ru",
        "cn" => "whois.cnnic.cn",
        "nl" => "whois.domain-registry.nl",
        "it" => "whois.nic.it",
        "es" => "whois.nic.es",
        "pl" => "whois.dns.pl",
        "se" => "whois.iis.se",
        "no" => "whois.norid.no",
        "dk" => "whois.dk-hostmaster.dk",
        "fi" => "whois.fi",
        "ch" => "whois.nic.ch",
        "at" => "whois.nic.at",
        "be" => "whois.dns.be",
        "eu" => "whois.eu",
        _ => null
    };

    private static ToolResult<WhoisReport> Cancelled() =>
        ToolResult<WhoisReport>.Fail(
            ToolErrorKind.Cancelled,
            WhoisErrorCodes.Cancelled,
            "WHOIS lookup cancelled by the user.");
}