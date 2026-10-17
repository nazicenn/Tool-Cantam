using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// IP bilgi sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz IP, ağ hatası ve "bulunamadı" sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IIpInfoService
{
    /// <summary>HTTP isteği zaman aşımı (ms).</summary>
    int QueryTimeoutMs { get; }

    /// <summary>IP adresi biçimini doğrular (IPv4 + IPv6).</summary>
    ToolResult<string> ValidateIp(string? ip);

    /// <summary>Belirtilen IP için coğrafi ve ağ bilgisi getirir.</summary>
    Task<ToolResult<IpInfoReport>> GetIpInfoAsync(
        string? ip,
        CancellationToken cancellationToken = default);
}

/// <summary>IP bilgisinin toplam sonucu.</summary>
public sealed record IpInfoReport(
    string IpAddress,
    string AddressFamily,
    string Country,
    string CountryCode,
    string Region,
    string RegionCode,
    string City,
    string Zip,
    double Latitude,
    double Longitude,
    string Timezone,
    string Isp,
    string Org,
    string Asn,
    TimeSpan Elapsed)
{
    public string Coordinates => $"{Latitude:F4}, {Longitude:F4}";
    public string Location => string.IsNullOrEmpty(City)
        ? Country
        : $"{City}, {Region}, {Country}";
}