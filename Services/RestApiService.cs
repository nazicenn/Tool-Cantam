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

/// <summary>REST API client — GET/POST/PUT/DELETE/PATCH/HEAD.</summary>
public sealed class RestApiService : IRestApiService
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

    public async Task<ToolResult<RestApiReport>> SendAsync(RestApiOptions o, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(o?.Url))
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Validation, RestApiErrorCodes.EmptyUrl, "URL is empty.");
        if (!Uri.TryCreate(o.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Validation, RestApiErrorCodes.InvalidUrl, "Invalid http/https URL.");

        var validMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD" };
        var method = (o.Method ?? "GET").ToUpperInvariant();
        if (Array.IndexOf(validMethods, method) < 0)
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Validation, RestApiErrorCodes.InvalidMethod, $"Invalid method: {method}");

        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            using var req = new HttpRequestMessage(new HttpMethod(method), uri);

            if (!string.IsNullOrWhiteSpace(o.Headers))
            {
                foreach (var line in o.Headers.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    var idx = trimmed.IndexOf(':');
                    if (idx <= 0) continue;
                    req.Headers.TryAddWithoutValidation(trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim());
                }
            }

            if (!string.IsNullOrWhiteSpace(o.Body) && method is "POST" or "PUT" or "PATCH" or "DELETE")
            {
                req.Content = new StringContent(o.Body, Encoding.UTF8);
                if (!req.Content.Headers.Contains("Content-Type"))
                    req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            }

            using var resp = await Client.SendAsync(req, HttpCompletionOption.ResponseContentRead, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .Select(h => new KeyValuePair<string, string>(h.Key, string.Join(", ", h.Value)))
                .ToList();

            sw.Stop();
            return ToolResult<RestApiReport>.Ok(new RestApiReport(
                (int)resp.StatusCode, resp.ReasonPhrase ?? "", body, headers, sw.Elapsed, body.Length));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Unexpected, RestApiErrorCodes.Timeout, "Timeout.");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Unexpected, RestApiErrorCodes.HttpError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<RestApiReport>.Fail(ToolErrorKind.Unexpected, RestApiErrorCodes.Unexpected, ex.Message);
        }
    }
}