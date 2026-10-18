using tool_cantam.Services.ErrorCodes;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// IP bilgi sorgusunun tek uygulamasý. HTTP istekleri ip-api.com üzerinden yapýlýr.
/// </summary>
public sealed class IpInfoService : IIpInfoService, IDisposable
{
    public const int QueryTimeoutMsLimit = 5000;

    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public IpInfoService()
        : this(new HttpClient { Timeout = TimeSpan.FromMilliseconds(QueryTimeoutMsLimit) }, ownsClient: true)
    {
    }

    public IpInfoService(HttpClient client, bool ownsClient = false)
    {
        _client = client;
        _ownsClient = ownsClient;
    }

    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    public ToolResult<string> ValidateIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                IpInfoErrorCodes.InvalidIp,
                "IP address is empty.");
        }

        string trimmed = ip.Trim();

        if (!IPAddress.TryParse(trimmed, out IPAddress? parsed))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                IpInfoErrorCodes.InvalidIp,
                $"'{trimmed}' is not a valid IP address.");
        }

        return ToolResult<string>.Ok(parsed.ToString());
    }

    public async Task<ToolResult<IpInfoReport>> GetIpInfoAsync(
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateIp(ip);
        if (!validation.IsSuccess)
            return ToolResult<IpInfoReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string address = validation.Value!;
        string family = IPAddress.TryParse(address, out var parsedIp)
            ? (parsedIp.AddressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6")
            : "Unknown";

        var stopwatch = Stopwatch.StartNew();

        try
        {
            string url = $"http://ip-api.com/json/{address}" +
                         "?fields=status,message,country,countryCode,region,regionName,city,zip," +
                         "lat,lon,timezone,isp,org,as,query";

            using var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == (HttpStatusCode)429)
            {
                stopwatch.Stop();
                return ToolResult<IpInfoReport>.Fail(
                    ToolErrorKind.Validation,
                    IpInfoErrorCodes.RateLimited,
                    "Rate limit exceeded (45 requests/min).");
            }

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                return ToolResult<IpInfoReport>.Fail(
                    ToolErrorKind.Unexpected,
                    IpInfoErrorCodes.NetworkError,
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            IpApiResponse? payload = await response.Content
                .ReadFromJsonAsync<IpApiResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();

            if (payload is null)
            {
                return ToolResult<IpInfoReport>.Fail(
                    ToolErrorKind.Unexpected,
                    IpInfoErrorCodes.NetworkError,
                    "Empty response from ip-api.com.");
            }

            if (!string.Equals(payload.Status, "success", StringComparison.OrdinalIgnoreCase))
            {
                return ToolResult<IpInfoReport>.Fail(
                    ToolErrorKind.NotFound,
                    IpInfoErrorCodes.NotFound,
                    payload.Message ?? "IP not found.");
            }

            string asn = payload.As ?? string.Empty;
            int spaceIdx = asn.IndexOf(' ');
            string asnNumber = spaceIdx > 0 ? asn[..spaceIdx] : asn;

            return ToolResult<IpInfoReport>.Ok(new IpInfoReport(
                payload.Query ?? address,
                family,
                payload.Country ?? "",
                payload.CountryCode ?? "",
                payload.RegionName ?? payload.Region ?? "",
                payload.Region ?? "",
                payload.City ?? "",
                payload.Zip ?? "",
                payload.Lat,
                payload.Lon,
                payload.Timezone ?? "",
                payload.Isp ?? "",
                payload.Org ?? "",
                asnNumber,
                stopwatch.Elapsed));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return ToolResult<IpInfoReport>.Fail(
                ToolErrorKind.Unexpected,
                IpInfoErrorCodes.QueryTimeout,
                $"HTTP request timed out after {QueryTimeoutMsLimit} ms.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return ToolResult<IpInfoReport>.Fail(
                ToolErrorKind.Unexpected,
                IpInfoErrorCodes.NetworkError,
                ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<IpInfoReport>.Fail(
                ToolErrorKind.Unexpected,
                IpInfoErrorCodes.Unexpected,
                ex.ToString());
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
            _client.Dispose();
    }

    private static ToolResult<IpInfoReport> Cancelled() =>
        ToolResult<IpInfoReport>.Fail(
            ToolErrorKind.Cancelled,
            IpInfoErrorCodes.Cancelled,
            "IP lookup cancelled by the user.");

    private sealed class IpApiResponse
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("regionName")] public string? RegionName { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
        [JsonPropertyName("lat")] public double Lat { get; set; }
        [JsonPropertyName("lon")] public double Lon { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("isp")] public string? Isp { get; set; }
        [JsonPropertyName("org")] public string? Org { get; set; }
        [JsonPropertyName("as")] public string? As { get; set; }
        [JsonPropertyName("query")] public string? Query { get; set; }
    }
}