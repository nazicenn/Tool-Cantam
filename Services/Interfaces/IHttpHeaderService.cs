using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// HTTP başlık analizi sözleşmesi. HEAD dener, desteklenmezse GET'e düşer.
/// </summary>
public interface IHttpHeaderService
{
    TimeSpan RequestTimeout { get; }
    int MaxHeaders { get; }

    bool IsValidUrl(string? url);

    Task<ToolResult<HttpHeaderReport>> AnalyzeAsync(string? url, CancellationToken cancellationToken);
}

/// <summary>Başlık kategorisi (renklendirme ve gruplama için).</summary>
public enum HeaderCategory
{
    Security,
    Cache,
    Server,
    Content,
    Cors,
    Other
}

/// <summary>Tek bir başlık satırı.</summary>
public sealed record HttpHeaderEntry(string Name, string Value, HeaderCategory Category);

/// <summary>Analiz raporu.</summary>
public sealed record HttpHeaderReport(
    string Url,
    int StatusCode,
    string ReasonPhrase,
    string Server,
    long? ContentLength,
    IReadOnlyList<HttpHeaderEntry> Headers,
    TimeSpan Elapsed,
    bool UsedGetFallback);