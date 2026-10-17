using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// MAC adresi satıcı sorgusunun tek uygulaması. maclookup.app API'si üzerinden OUI aranır.
/// </summary>
public sealed class MacLookupService : IMacLookupService, IDisposable
{
    public const int QueryTimeoutMsLimit = 8000;

    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public MacLookupService()
        : this(new HttpClient { Timeout = TimeSpan.FromMilliseconds(QueryTimeoutMsLimit) }, ownsClient: true)
    {
    }

    public MacLookupService(HttpClient client, bool ownsClient = false)
    {
        _client = client;
        _ownsClient = ownsClient;
    }

    public int QueryTimeoutMs => QueryTimeoutMsLimit;

    public ToolResult<string> NormalizeMac(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                MacLookupErrorCodes.InvalidMac,
                "MAC address is empty.");

        string clean = mac.Trim()
            .Replace(":", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace(" ", "")
            .ToUpperInvariant();

        if (clean.Length != 12)
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                MacLookupErrorCodes.InvalidMac,
                $"MAC address must contain 12 hex digits (got {clean.Length}).");

        foreach (char c in clean)
        {
            bool isHex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F');
            if (!isHex)
                return ToolResult<string>.Fail(
                    ToolErrorKind.Validation,
                    MacLookupErrorCodes.InvalidMac,
                    $"Non-hexadecimal character: '{c}'.");
        }

        return ToolResult<string>.Ok(clean);
    }

    public async Task<ToolResult<MacLookupReport>> LookupVendorAsync(
        string? mac,
        CancellationToken cancellationToken = default)
    {
        var validation = NormalizeMac(mac);
        if (!validation.IsSuccess)
            return ToolResult<MacLookupReport>.Fail(
                validation.ErrorKind, validation.ErrorCode!, validation.TechnicalDetail!);

        string normalized = validation.Value!;
        string formatted = FormatMac(normalized);
        string oui = normalized[..6];

        // İlk byte'ın bitleri:
        // bit 0 (LSB) = 1 → multicast, 0 → unicast
        // bit 1 = 1 → locally administered, 0 → universal
        byte firstByte = Convert.ToByte(normalized[..2], 16);
        bool isMulticast = (firstByte & 0x01) == 0x01;
        bool isLocallyAdministered = (firstByte & 0x02) == 0x02;

        // Multicast veya locally administered → OUI lookup yapma
        if (isMulticast || isLocallyAdministered)
        {
            var syntheticReport = new MacLookupReport(
                normalized, formatted, oui,
                isMulticast ? "Multicast Address" : "Locally Administered Address",
                "", "",
                isMulticast, isLocallyAdministered,
                TimeSpan.Zero);

            return ToolResult<MacLookupReport>.Ok(syntheticReport);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            string url = $"https://api.maclookup.app/v2/macs/{normalized}";
            using var response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == (HttpStatusCode)429)
            {
                stopwatch.Stop();
                return ToolResult<MacLookupReport>.Fail(
                    ToolErrorKind.Validation,
                    MacLookupErrorCodes.RateLimited,
                    "Rate limit exceeded. Try again in a moment.");
            }

            if (!response.IsSuccessStatusCode)
            {
                stopwatch.Stop();
                return ToolResult<MacLookupReport>.Fail(
                    ToolErrorKind.Unexpected,
                    MacLookupErrorCodes.NetworkError,
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            MacLookupApiResponse? payload = await response.Content
                .ReadFromJsonAsync<MacLookupApiResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();

            if (payload is null)
            {
                return ToolResult<MacLookupReport>.Fail(
                    ToolErrorKind.Unexpected,
                    MacLookupErrorCodes.NetworkError,
                    "Empty response from maclookup.app.");
            }

            string vendor = payload.Company ?? string.Empty;
            if (string.IsNullOrWhiteSpace(vendor) || string.Equals(vendor, "N/A", StringComparison.OrdinalIgnoreCase))
            {
                return ToolResult<MacLookupReport>.Fail(
                    ToolErrorKind.NotFound,
                    MacLookupErrorCodes.VendorNotFound,
                    $"No vendor found for OUI {oui}.");
            }

            return ToolResult<MacLookupReport>.Ok(new MacLookupReport(
                normalized, formatted, oui,
                vendor,
                payload.Address ?? "",
                payload.Country ?? "",
                isMulticast, isLocallyAdministered,
                stopwatch.Elapsed));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled();
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return ToolResult<MacLookupReport>.Fail(
                ToolErrorKind.Unexpected,
                MacLookupErrorCodes.QueryTimeout,
                $"HTTP request timed out after {QueryTimeoutMsLimit} ms.");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return ToolResult<MacLookupReport>.Fail(
                ToolErrorKind.Unexpected,
                MacLookupErrorCodes.NetworkError,
                ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<MacLookupReport>.Fail(
                ToolErrorKind.Unexpected,
                MacLookupErrorCodes.Unexpected,
                ex.ToString());
        }
    }

    private static string FormatMac(string normalized)
    {
        return string.Join(":",
            normalized[..2],
            normalized[2..4],
            normalized[4..6],
            normalized[6..8],
            normalized[8..10],
            normalized[10..12]);
    }

    public void Dispose()
    {
        if (_ownsClient)
            _client.Dispose();
    }

    private static ToolResult<MacLookupReport> Cancelled() =>
        ToolResult<MacLookupReport>.Fail(
            ToolErrorKind.Cancelled,
            MacLookupErrorCodes.Cancelled,
            "MAC lookup cancelled by the user.");

    private sealed class MacLookupApiResponse
    {
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
    }
}