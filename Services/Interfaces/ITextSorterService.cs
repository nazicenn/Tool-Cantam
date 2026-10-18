using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>Metin satırlarını sıralama sözleşmesi.</summary>
public interface ITextSorterService
{
    int MaxTextLength { get; }
    Task<ToolResult<TextSorterReport>> SortAsync(
        string? input, TextSorterOptions options, CancellationToken cancellationToken);
}

public enum SortDirection { Ascending, Descending }
public enum SortMode { Ordinal, Invariant }

public sealed record TextSorterOptions(
    SortDirection Direction,
    SortMode Mode,
    bool IgnoreCase,
    bool RemoveEmptyLines,
    bool TrimLines,
    bool RemoveDuplicates);

public sealed record TextSorterReport(
    string Output,
    int InputLines,
    int OutputLines,
    int RemovedEmpty,
    int RemovedDuplicates,
    TimeSpan Elapsed);