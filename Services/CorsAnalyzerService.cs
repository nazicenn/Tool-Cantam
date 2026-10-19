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

/// <summary>CORS baþlýk analizi — eksik, güvensiz veya tutarsýz yapýlandýrmalarý bulur.</summary>
public sealed class CorsAnalyzerService : ICorsAnalyzerService
{
    public const int MaxInputLengthLimit = 100 * 1024; // 100 KB
    public int MaxInputLength => MaxInputLengthLimit;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = Timeout };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ToolCantam/1.0");
        return c;
    }

    public async Task<ToolResult<CorsReport>> FetchAndAnalyzeAsync(string? url, CorsOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Validation, CorsAnalyzerErrorCodes.EmptyUrl, "URL is empty.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Validation, CorsAnalyzerErrorCodes.InvalidUrl, "Invalid http/https URL.");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);

            using var req = new HttpRequestMessage(HttpMethod.Options, uri);
            req.Headers.Add("Origin", "https://example.com");
            req.Headers.Add("Access-Control-Request-Method", "GET");

            using var resp = await Client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .Select(h => (h.Key, Value: string.Join(", ", h.Value)))
                .ToList();

            var sb = new System.Text.StringBuilder();
            foreach (var (k, v) in headers) sb.AppendLine($"{k}: {v}");
            return await AnalyzeAsync(sb.ToString(), options, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (OperationCanceledException)
        {
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Unexpected, CorsAnalyzerErrorCodes.Timeout, "Timeout.");
        }
        catch (HttpRequestException ex)
        {
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Unexpected, CorsAnalyzerErrorCodes.HttpError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<CorsReport>.Fail(ToolErrorKind.Unexpected, CorsAnalyzerErrorCodes.Unexpected, ex.Message);
        }
    }

    public Task<ToolResult<CorsReport>> AnalyzeAsync(string? headers, CorsOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(headers))
            return Fail<CorsReport>(ToolErrorKind.Validation, CorsAnalyzerErrorCodes.EmptyInput, "Headers are empty.");
        if (headers.Length > MaxInputLengthLimit)
            return Fail<CorsReport>(ToolErrorKind.TooLarge, CorsAnalyzerErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var parsed = ParseHeaders(headers);
            var issues = Analyze(parsed, options);

            var isValid = !issues.Any(i => i.Severity == CorsSeverity.Error);
            var status = isValid
                ? (issues.Count == 0 ? "OK" : "Warnings")
                : "Errors";

            sw.Stop();
            return Task.FromResult(ToolResult<CorsReport>.Ok(new CorsReport(
                headers, parsed, issues, isValid, status, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<CorsReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<CorsReport>(ToolErrorKind.Unexpected, CorsAnalyzerErrorCodes.Unexpected, ex.Message); }
    }

    private static List<CorsHeader> ParseHeaders(string input)
    {
        var list = new List<CorsHeader>();
        foreach (var raw in input.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var name = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            var kind = Categorize(name);
            list.Add(new CorsHeader(name, value, kind));
        }
        return list;
    }

    private static CorsHeaderKind Categorize(string name)
    {
        var n = name.ToLowerInvariant();
        return n switch
        {
            "access-control-allow-origin" => CorsHeaderKind.AllowOrigin,
            "access-control-allow-methods" => CorsHeaderKind.AllowMethods,
            "access-control-allow-headers" => CorsHeaderKind.AllowHeaders,
            "access-control-expose-headers" => CorsHeaderKind.ExposeHeaders,
            "access-control-allow-credentials" => CorsHeaderKind.AllowCredentials,
            "access-control-max-age" => CorsHeaderKind.MaxAge,
            _ => CorsHeaderKind.Other
        };
    }

    private static List<CorsIssue> Analyze(List<CorsHeader> headers, CorsOptions options)
    {
        var issues = new List<CorsIssue>();
        var allowOrigin = headers.FirstOrDefault(h => h.Kind == CorsHeaderKind.AllowOrigin);
        var allowCreds = headers.FirstOrDefault(h => h.Kind == CorsHeaderKind.AllowCredentials);
        var allowMethods = headers.FirstOrDefault(h => h.Kind == CorsHeaderKind.AllowMethods);
        var allowHeaders = headers.FirstOrDefault(h => h.Kind == CorsHeaderKind.AllowHeaders);

        // Access-Control-Allow-Origin zorunlu
        if (allowOrigin is null)
        {
            if (options.TreatMissingAsError)
                issues.Add(new CorsIssue("missing-origin", "Access-Control-Allow-Origin header is missing.", CorsSeverity.Error));
            else
                issues.Add(new CorsIssue("missing-origin", "Access-Control-Allow-Origin header is missing.", CorsSeverity.Warning));
        }
        else if (allowOrigin.Value == "*")
        {
            if (allowCreds is not null && allowCreds.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                issues.Add(new CorsIssue("wildcard-with-creds",
                    "Wildcard origin with credentials is not allowed by browsers.", CorsSeverity.Error));
            else
                issues.Add(new CorsIssue("wildcard",
                    "Wildcard origin allows any website to read responses.", CorsSeverity.Warning));
        }

        // Credentials + wildcard origin
        if (allowCreds is not null && allowCreds.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            if (allowOrigin?.Value == "*")
                issues.Add(new CorsIssue("creds-wildcard",
                    "Wildcard origin cannot be used with credentials.", CorsSeverity.Error));
        }

        // Methods kontrolü
        if (allowMethods is null && allowOrigin is not null)
        {
            issues.Add(new CorsIssue("missing-methods",
                "Access-Control-Allow-Methods is missing; preflight may fail.", CorsSeverity.Info));
        }

        // Headers kontrolü
        if (allowHeaders is null && allowOrigin is not null)
        {
            issues.Add(new CorsIssue("missing-headers",
                "Access-Control-Allow-Headers is missing; preflight may fail.", CorsSeverity.Info));
        }

        // Vary: Origin kontrolü (credentials varken)
        var vary = headers.FirstOrDefault(h => h.Name.Equals("Vary", StringComparison.OrdinalIgnoreCase));
        if (allowCreds?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) == true &&
            (vary is null || !vary.Value.Contains("Origin", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new CorsIssue("missing-vary",
                "Vary: Origin is missing; caches may serve wrong origin.", CorsSeverity.Warning));
        }

        return issues;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}
