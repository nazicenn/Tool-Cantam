using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>Regex test sözleşmesi.</summary>
public interface IRegexTesterService
{
    int MaxInputLength { get; }
    int MaxMatchCount { get; }
    TimeSpan RegexTimeout { get; }

    Task<ToolResult<RegexReport>> TestAsync(
        string? pattern, string? input,
        RegexTestOptions options, CancellationToken cancellationToken);

    IReadOnlyList<RegexSample> GetSamples();
}

public sealed record RegexTestOptions(
    bool IgnoreCase,
    bool Multiline,
    bool Singleline,
    bool ExplicitCapture,
    bool IgnorePatternWhitespace,
    bool ReplaceMode,
    string? ReplaceWith);

public sealed record RegexMatch(
    int Index,
    int Length,
    string Value,
    IReadOnlyList<string> Groups);

public sealed record RegexReport(
    IReadOnlyList<RegexMatch> Matches,
    string Highlighted,
    string Replaced,
    int MatchCount,
    TimeSpan Elapsed);

public sealed record RegexSample(string Key, string Pattern, string Input, string ReplaceWith);