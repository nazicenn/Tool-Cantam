using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICorsAnalyzerService
{
    int MaxInputLength { get; }
    Task<ToolResult<CorsReport>> AnalyzeAsync(string? headers, CorsOptions options, CancellationToken ct);
    Task<ToolResult<CorsReport>> FetchAndAnalyzeAsync(string? url, CorsOptions options, CancellationToken ct);
}

public sealed record CorsOptions(bool TreatMissingAsError);

public sealed record CorsHeader(string Name, string Value, CorsHeaderKind Kind);

public enum CorsHeaderKind { AllowOrigin, AllowMethods, AllowHeaders, ExposeHeaders, AllowCredentials, MaxAge, Other }

public sealed record CorsIssue(string Code, string Message, CorsSeverity Severity);

public enum CorsSeverity { Info, Warning, Error }

public sealed record CorsReport(
    string Input,
    IReadOnlyList<CorsHeader> Headers,
    IReadOnlyList<CorsIssue> Issues,
    bool IsValid,
    string Status,
    TimeSpan Elapsed);