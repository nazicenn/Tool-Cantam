using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// MAC adresi satıcı sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz MAC, ağ hatası ve bulunamadı sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IMacLookupService
{
    /// <summary>HTTP isteği zaman aşımı (ms).</summary>
    int QueryTimeoutMs { get; }

    /// <summary>MAC adresi biçimini doğrular ve normalize eder.</summary>
    ToolResult<string> NormalizeMac(string? mac);

    /// <summary>Belirtilen MAC adresi için OUI satıcısını sorgular.</summary>
    Task<ToolResult<MacLookupReport>> LookupVendorAsync(
        string? mac,
        CancellationToken cancellationToken = default);
}

/// <summary>MAC lookup sonucu.</summary>
public sealed record MacLookupReport(
    string NormalizedMac,
    string FormattedMac,
    string Oui,
    string Vendor,
    string Address,
    string Country,
    bool IsMulticast,
    bool IsLocallyAdministered,
    TimeSpan Elapsed)
{
    public string MacType => IsMulticast
        ? "Multicast"
        : IsLocallyAdministered ? "Locally Administered" : "Unicast (Universal)";
}