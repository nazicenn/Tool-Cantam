using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IRestApiService
{
    TimeSpan RequestTimeout { get; }
    Task<ToolResult<RestApiReport>> SendAsync(RestApiOptions options, CancellationToken ct);
}

public sealed record RestApiOptions(string Url, string Method, string Headers, string? Body);
public sealed record RestApiReport(
    int StatusCode, string ReasonPhrase, string Body,
    IReadOnlyList<KeyValuePair<string, string>> Headers,
    TimeSpan Elapsed, long BodyBytes);