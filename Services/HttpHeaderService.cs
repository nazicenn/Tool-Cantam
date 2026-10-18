using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Statik HttpClient (tek örnek, socket exhaustion yok) ile HTTP başlıklarını analiz eder.
/// Önce HEAD dener; 403/405/501 dönerse GET'e düşer. User-Agent gönderir.
/// </summary>
public sealed class HttpHeaderService : IHttpHeaderService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private const int MaxHeaderCount = 200;

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = Timeout };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ToolCantam/1.0 (+https://github.com/)");
        c.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        return c;
    }

    public TimeSpan RequestTimeout => Timeout;
    public int MaxHeaders => MaxHeaderCount;

    public bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public async Task<ToolResult<HttpHeaderReport>> AnalyzeAsync(string? url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Validation,
                HttpHeaderErrorCodes.EmptyUrl, "URL is empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Validation,
                HttpHeaderErrorCodes.InvalidUrl, $"Invalid URL: '{url}'.");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Validation,
                HttpHeaderErrorCodes.UnsupportedScheme, $"Scheme '{uri.Scheme}' not allowed.");

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            HttpResponseMessage response;
            bool usedGet = false;
            try
            {
                var headReq = new HttpRequestMessage(HttpMethod.Head, uri);
                response = await Client.SendAsync(headReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                    response.StatusCode == System.Net.HttpStatusCode.NotImplemented ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    response.Dispose();
                    usedGet = true;
                    var getReq = new HttpRequestMessage(HttpMethod.Get, uri);
                    response = await Client.SendAsync(getReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                }
            }
            catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested)
            {
                // HEAD başarısız → GET dene
                usedGet = true;
                var getReq = new HttpRequestMessage(HttpMethod.Get, uri);
                response = await Client.SendAsync(getReq, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            }

            using (response)
            {
                cts.Token.ThrowIfCancellationRequested();

                var entries = new List<HttpHeaderEntry>();
                AppendHeaders(entries, response.Headers.Select(h => (h.Key, string.Join(", ", h.Value))));
                if (response.Content is not null)
                    AppendHeaders(entries, response.Content.Headers.Select(h => (h.Key, string.Join(", ", h.Value))));

                entries.Insert(0, new HttpHeaderEntry(":status",
                    $"{(int)response.StatusCode} {response.ReasonPhrase}", HeaderCategory.Server));

                sw.Stop();

                var report = new HttpHeaderReport(
                    uri.ToString(),
                    (int)response.StatusCode,
                    response.ReasonPhrase ?? string.Empty,
                    response.Headers.Server.ToString() ?? string.Empty,
                    response.Content?.Headers.ContentLength,
                    entries,
                    sw.Elapsed,
                    usedGet);

                return ToolResult<HttpHeaderReport>.Ok(report);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Cancelled,
                "L.Common.Error.Cancelled", "Cancelled by user.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Unexpected,
                HttpHeaderErrorCodes.Timeout, $"Request timed out after {Timeout.TotalSeconds}s.");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Unexpected,
                HttpHeaderErrorCodes.HttpError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Unexpected,
                HttpHeaderErrorCodes.Unexpected, ex.Message);
        }
    }

    private static void AppendHeaders(List<HttpHeaderEntry> target, IEnumerable<(string Name, string Value)> source)
    {
        foreach (var (name, value) in source)
        {
            if (target.Count >= MaxHeaderCount) break;
            target.Add(new HttpHeaderEntry(name, value, Categorize(name)));
        }
    }

    private static HeaderCategory Categorize(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.StartsWith("strict-transport") || n.StartsWith("content-security") ||
            n.StartsWith("x-frame") || n.StartsWith("x-content-type") ||
            n == "referrer-policy" || n == "permissions-policy")
            return HeaderCategory.Security;
        if (n == "cache-control" || n == "expires" || n == "etag" || n == "last-modified" || n == "age")
            return HeaderCategory.Cache;
        if (n == "server" || n == "x-powered-by" || n == "via")
            return HeaderCategory.Server;
        if (n.StartsWith("access-control-"))
            return HeaderCategory.Cors;
        if (n == "content-type" || n == "content-length" || n == "content-encoding" || n == "content-language")
            return HeaderCategory.Content;
        return HeaderCategory.Other;
    }
}