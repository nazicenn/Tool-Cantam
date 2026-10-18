using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IGeoIpLookupService
{
    TimeSpan RequestTimeout { get; }
    Task<ToolResult<GeoIpReport>> LookupAsync(string? ip, CancellationToken ct);
    Task<ToolResult<string>> GetMyIpAsync(CancellationToken ct);
}

public sealed record GeoIpReport(
    string IpAddress, string Country, string CountryCode,
    string Region, string City, string Zip,
    double Lat, double Lon, string Timezone,
    string Isp, string Org, bool IsProxy);