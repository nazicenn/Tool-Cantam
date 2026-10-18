using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IHttpRequestTesterService
{
    TimeSpan RequestTimeout { get; }
    Task<ToolResult<HttpResponseReport>> SendAsync(HttpTesterOptions options, CancellationToken ct);
}

public sealed record HttpTesterOptions(
    string Url, string Method, string Headers, string? Body);

public sealed record HttpResponseReport(
    int StatusCode, string ReasonPhrase, string Body,
    IReadOnlyList<KeyValuePair<string, string>> Headers,
    TimeSpan Elapsed, long BodyBytes);