using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Ping (ICMP echo) sorgu sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// iptal, geçersiz hedef, DNS hatası ve ICMP engeli sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IPingService
{
    int MaxCount { get; }
    int MinTimeoutMs { get; }
    int MaxTimeoutMs { get; }

    ToolResult<string> ValidateTarget(string? target);

    Task<ToolResult<PingReport>> PingAsync(
        string? target,
        int count,
        int timeoutMs,
        IProgress<PingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Tek bir ping denemesinin sonucu.</summary>
public sealed record PingAttempt(int Sequence, string Status, long? RoundtripMs, string Address);

/// <summary>Ping raporunun tamamı.</summary>
public sealed record PingReport(
    string Target,
    string ResolvedAddress,
    int Sent,
    int Received,
    int Lost,
    int PacketLossPercent,
    long? MinMs,
    long? MaxMs,
    long? AvgMs,
    IReadOnlyList<PingAttempt> Attempts,
    TimeSpan Elapsed);

/// <summary>İlerleme raporu.</summary>
public sealed record PingProgress(int Completed, int Total, int Received);