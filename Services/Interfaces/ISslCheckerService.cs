using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// SSL sertifika sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz domain, bağlantı hatası ve sertifika hatası sınıflandırılmış
/// hata olarak bildirilir.
/// </summary>
public interface ISslCheckerService
{
    /// <summary>Bağlantı zaman aşımı (ms).</summary>
    int QueryTimeoutMs { get; }

    /// <summary>Domain biçimini doğrular.</summary>
    ToolResult<string> ValidateDomain(string? domain);

    /// <summary>Belirtilen domain için SSL sertifikasını kontrol eder.</summary>
    Task<ToolResult<SslCertificateReport>> CheckAsync(
        string? domain,
        CancellationToken cancellationToken = default);
}

/// <summary>SSL sertifikasının toplam raporu.</summary>
public sealed record SslCertificateReport(
    string Domain,
    string Subject,
    string Issuer,
    string CommonName,
    DateTime NotBefore,
    DateTime NotAfter,
    int RemainingDays,
    string SerialNumber,
    string SignatureAlgorithm,
    string TlsProtocol,
    bool IsExpired,
    bool IsNotYetValid,
    bool IsHostnameMatch,
    bool IsChainTrusted,
    bool IsValid,
    string Status,
    IReadOnlyList<string> ChainIssues,
    TimeSpan Elapsed)
{
    public string ValidityText => $"{NotBefore:yyyy-MM-dd} → {NotAfter:yyyy-MM-dd}";
}