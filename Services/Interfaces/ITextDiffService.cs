using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>Metin karşılaştırma (diff) sözleşmesi.</summary>
public interface ITextDiffService
{
    int MaxInputLength { get; }
    Task<ToolResult<DiffReport>> CompareAsync(
        string? left, string? right, DiffOptions options, CancellationToken cancellationToken);
}

public enum DiffLineKind { Unchanged, Added, Removed, Changed }

public sealed record DiffOptions(
    bool IgnoreCase,
    bool IgnoreWhitespace,
    bool IgnoreEmptyLines);

public sealed record DiffLine(
    DiffLineKind Kind,
    string? Left,
    string? Right,
    int LeftIndex,
    int RightIndex);

public sealed record DiffReport(
    IReadOnlyList<DiffLine> Lines,
    int Added,
    int Removed,
    int Changed,
    int Unchanged,
    TimeSpan Elapsed);