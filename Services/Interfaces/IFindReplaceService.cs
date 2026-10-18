using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>Metin bul-değiştir sözleşmesi.</summary>
public interface IFindReplaceService
{
    int MaxInputLength { get; }
    Task<ToolResult<FindReplaceReport>> ReplaceAsync(
        string? input, string? find, string? replace,
        FindReplaceOptions options, CancellationToken cancellationToken);
}

public sealed record FindReplaceOptions(
    bool MatchCase,
    bool MatchWholeWord,
    bool UseRegex);

public sealed record FindReplaceReport(
    string Output,
    int MatchCount,
    int ReplaceCount,
    string Pattern,
    TimeSpan Elapsed);