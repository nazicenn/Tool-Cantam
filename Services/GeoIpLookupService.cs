using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>ip-api.com ile GeoIP sorgulama (45/dk limit).</summary>
public sealed class GeoIpLookupService : IGeoIpLookupService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = Timeout };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ToolCantam/1.0");
        return c;
    }

    public TimeSpan RequestTimeout => Timeout;

    public async Task<ToolResult<GeoIpReport>> LookupAsync(string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Validation, GeoIpLookupErrorCodes.EmptyIp, "IP is empty.");
        if (!IPAddress.TryParse(ip.Trim(), out _))
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Validation, GeoIpLookupErrorCodes.InvalidIp, "Invalid IP.");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            var url = $"http://ip-api.com/json/{ip.Trim()}?fields=status,message,country,countryCode,regionName,city,zip,lat,lon,timezone,isp,org,proxy,query";
            var json = await Client.GetStringAsync(url, cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "success")
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "Unknown" : "Unknown";
                return ToolResult<GeoIpReport>.Fail(ToolErrorKind.NotFound, GeoIpLookupErrorCodes.NotFound, msg);
            }

            var report = new GeoIpReport(
                root.GetProperty("query").GetString() ?? ip,
                root.GetProperty("country").GetString() ?? "",
                root.GetProperty("countryCode").GetString() ?? "",
                root.GetProperty("regionName").GetString() ?? "",
                root.GetProperty("city").GetString() ?? "",
                root.GetProperty("zip").GetString() ?? "",
                root.GetProperty("lat").GetDouble(),
                root.GetProperty("lon").GetDouble(),
                root.GetProperty("timezone").GetString() ?? "",
                root.GetProperty("isp").GetString() ?? "",
                root.GetProperty("org").GetString() ?? "",
                root.GetProperty("proxy").GetBoolean());

            return ToolResult<GeoIpReport>.Ok(report);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.Timeout, "Timeout.");
        }
        catch (HttpRequestException)
        {
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.NetworkError, "Network error.");
        }
        catch (Exception ex)
        {
            return ToolResult<GeoIpReport>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.Unexpected, ex.Message);
        }
    }

    public async Task<ToolResult<string>> GetMyIpAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            var ip = (await Client.GetStringAsync("https://api.ipify.org", cts.Token)).Trim();
            return ToolResult<string>.Ok(ip);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<string>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<string>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.Timeout, "Timeout.");
        }
        catch (HttpRequestException)
        {
            return ToolResult<string>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.NetworkError, "Network error.");
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(ToolErrorKind.Unexpected, GeoIpLookupErrorCodes.Unexpected, ex.Message);
        }
    }
}