using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Unix timestamp ↔ DateTime dönüşümü sözleşmesi.
/// Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner; exception fırlatılmaz.
/// </summary>
public interface IUnixTimestampService
{
    long MinSeconds { get; }
    long MaxSeconds { get; }

    Task<ToolResult<UnixTimestampReport>> FromUnixAsync(
        string? input,
        TimestampUnit unit,
        TimeDisplayMode mode,
        CancellationToken cancellationToken);

    Task<ToolResult<UnixTimestampReport>> ToUnixAsync(
        string? input,
        TimeDisplayMode mode,
        CancellationToken cancellationToken);

    UnixTimestampReport Now();
}

/// <summary>Timestamp birimi.</summary>
public enum TimestampUnit
{
    Seconds,
    Milliseconds
}

/// <summary>Görüntüleme modu.</summary>
public enum TimeDisplayMode
{
    Utc,
    Local
}

/// <summary>Dönüşüm raporu — hem timestamp hem tarih hem formatlı çıktılar.</summary>
public sealed record UnixTimestampReport(
    long UnixSeconds,
    long UnixMilliseconds,
    DateTime Utc,
    DateTime Local,
    string Iso8601,
    string Rfc1123,
    string Readable,
    TimeDisplayMode Mode);