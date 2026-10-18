using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>HTTP isteğini cURL komutuna çevirir.</summary>
public sealed class CurlGeneratorService : ICurlGeneratorService
{
    public Task<ToolResult<CurlReport>> GenerateAsync(CurlOptions o, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(o?.Url))
            return Fail<CurlReport>(ToolErrorKind.Validation, CurlGeneratorErrorCodes.EmptyUrl, "URL is empty.");
        if (!Uri.TryCreate(o.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Fail<CurlReport>(ToolErrorKind.Validation, CurlGeneratorErrorCodes.InvalidUrl, "Invalid URL.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var method = (o.Method ?? "GET").ToUpperInvariant();
            var sb = new StringBuilder();
            sb.Append("curl");

            if (method != "GET")
            {
                if (o.UseLongFlags) sb.Append($" --request {method}");
                else sb.Append($" -X {method}");
            }

            sb.Append($" \"{EscapeForShell(o.Url)}\"");

            if (o.Insecure) sb.Append(o.UseLongFlags ? " --insecure" : " -k");

            if (o.IncludeHeaders && !string.IsNullOrWhiteSpace(o.Headers))
            {
                foreach (var line in o.Headers.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    var idx = trimmed.IndexOf(':');
                    if (idx <= 0) continue;
                    var key = trimmed[..idx].Trim();
                    var val = trimmed[(idx + 1)..].Trim();
                    sb.Append($" \\\n  {(o.UseLongFlags ? "--header" : "-H")} \"{EscapeForShell(key)}: {EscapeForShell(val)}\"");
                }
            }

            if (o.IncludeBody && !string.IsNullOrWhiteSpace(o.Body) && (method == "POST" || method == "PUT" || method == "PATCH" || method == "DELETE"))
            {
                if (o.UseLongFlags) sb.Append($" \\\n  --data \"{EscapeForShell(o.Body)}\"");
                else sb.Append($" \\\n  -d \"{EscapeForShell(o.Body)}\"");
            }

            sw.Stop();
            var cmd = sb.ToString();
            return Task.FromResult(ToolResult<CurlReport>.Ok(new CurlReport(cmd, cmd.Split('\n').Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<CurlReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<CurlReport>(ToolErrorKind.Unexpected, CurlGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static string EscapeForShell(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}