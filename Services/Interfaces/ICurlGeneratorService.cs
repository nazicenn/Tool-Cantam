using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICurlGeneratorService
{
    Task<ToolResult<CurlReport>> GenerateAsync(CurlOptions options, CancellationToken ct);
}

public sealed record CurlOptions(
    string Url, string Method, string Headers, string? Body,
    bool IncludeHeaders, bool IncludeBody,
    bool UseLongFlags, bool Insecure);

public sealed record CurlReport(string Command, int LineCount, TimeSpan Elapsed);