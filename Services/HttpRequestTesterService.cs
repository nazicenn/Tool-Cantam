using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Mini Postman — GET/POST/PUT/DELETE/PATCH.</summary>
public sealed class HttpRequestTesterService : IHttpRequestTesterService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = Timeout };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ToolCantam/1.0");
        return c;
    }

    public TimeSpan RequestTimeout => Timeout;

    public async Task<ToolResult<HttpResponseReport>> SendAsync(HttpTesterOptions o, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(o?.Url))
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Validation,
                HttpRequestTesterErrorCodes.EmptyUrl, "URL is empty.");

        if (!Uri.TryCreate(o.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Validation,
                HttpRequestTesterErrorCodes.InvalidUrl, "Invalid http/https URL.");

        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD" };
        var method = (o.Method ?? "GET").ToUpperInvariant();
        if (Array.IndexOf(validMethods, method) < 0)
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Validation,
                HttpRequestTesterErrorCodes.InvalidMethod, $"Invalid method: {method}");

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            using var req = new HttpRequestMessage(new HttpMethod(method), uri);
            ParseHeaders(req, o.Headers);

            if (!string.IsNullOrWhiteSpace(o.Body) && (method == "POST" || method == "PUT" || method == "PATCH"))
                req.Content = new StringContent(o.Body, Encoding.UTF8);
            if (req.Content is not null && !req.Content.Headers.Contains("Content-Type"))
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using var resp = await Client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .Select(h => new KeyValuePair<string, string>(h.Key, string.Join(", ", h.Value)))
                .ToList();

            sw.Stop();
            return ToolResult<HttpResponseReport>.Ok(new HttpResponseReport(
                (int)resp.StatusCode, resp.ReasonPhrase ?? "", body, headers, sw.Elapsed, body.Length));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Cancelled,
                "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Unexpected,
                HttpRequestTesterErrorCodes.Timeout, "Request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Unexpected,
                HttpRequestTesterErrorCodes.HttpError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<HttpResponseReport>.Fail(ToolErrorKind.Unexpected,
                HttpRequestTesterErrorCodes.Unexpected, ex.Message);
        }
    }

    private static void ParseHeaders(HttpRequestMessage req, string? headers)
    {
        if (string.IsNullOrWhiteSpace(headers)) return;
        foreach (var line in headers.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var idx = trimmed.IndexOf(':');
            if (idx <= 0) continue;
            try { req.Headers.TryAddWithoutValidation(trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim()); }
            catch { /* skip */ }
        }
    }
}