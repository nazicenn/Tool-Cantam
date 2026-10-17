using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// WHOIS sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz domain, WHOIS sunucusu bulunamadı ve ağ hatası sınıflandırılmış
/// hata olarak bildirilir.
/// </summary>
public interface IWhoisService
{
    /// <summary>WHOIS sunucusu ile bağlantı zaman aşımı (ms).</summary>
    int QueryTimeoutMs { get; }

    /// <summary>Domain biçimini doğrular.</summary>
    ToolResult<string> ValidateDomain(string? domain);

    /// <summary>Belirtilen domain için WHOIS sorgusu yapar.</summary>
    Task<ToolResult<WhoisReport>> LookupAsync(
        string? domain,
        CancellationToken cancellationToken = default);
}

/// <summary>WHOIS sorgusunun ayrıştırılmış sonucu.</summary>
public sealed record WhoisReport(
    string Domain,
    string Tld,
    string WhoisServer,
    string Registrar,
    string CreationDate,
    string ExpirationDate,
    string UpdatedDate,
    IReadOnlyList<string> NameServers,
    IReadOnlyList<string> Statuses,
    string RegistrantName,
    string RegistrantOrganization,
    string RegistrantCountry,
    string RegistrantEmail,
    TimeSpan Elapsed,
    string RawText)
{
    public string NameServersText => string.Join(", ", NameServers);
    public string StatusesText => string.Join(", ", Statuses);
    public string RegistrantText => !string.IsNullOrEmpty(RegistrantOrganization)
        ? RegistrantOrganization
        : RegistrantName;
}