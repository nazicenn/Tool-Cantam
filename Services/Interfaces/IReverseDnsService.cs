using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Reverse DNS (PTR) sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz IP ve PTR kaydı olmaması sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IReverseDnsService
{
    /// <summary>Sorgu başına zaman aşımı (ms).</summary>
    int QueryTimeoutMs { get; }

    /// <summary>IP adresi biçimini doğrular (IPv4 + IPv6).</summary>
    ToolResult<string> ValidateIp(string? ip);

    /// <summary>Belirtilen IP için PTR kaydını çözer.</summary>
    Task<ToolResult<ReverseDnsReport>> LookupAsync(
        string? ip,
        CancellationToken cancellationToken = default);
}

/// <summary>PTR çözümlemesinin sonucu.</summary>
public sealed record ReverseDnsReport(
    string IpAddress,
    string AddressFamily,
    string HostName,
    IReadOnlyList<string> Aliases,
    TimeSpan Elapsed);