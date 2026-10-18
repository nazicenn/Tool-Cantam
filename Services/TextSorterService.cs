using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Satır bazlı metin sıralama. InvariantCulture, Ordinal ve Invariant modları.
/// </summary>
public sealed class TextSorterService : ITextSorterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public Task<ToolResult<TextSorterReport>> SortAsync(
        string? input, TextSorterOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<TextSorterReport>(ToolErrorKind.Validation,
                TextSorterErrorCodes.EmptyInput, "Input is empty.");

        if (input.Length > MaxTextLengthLimit)
            return Fail<TextSorterReport>(ToolErrorKind.TooLarge,
                TextSorterErrorCodes.InputTooLarge,
                $"Input length {input.Length} exceeds {MaxTextLengthLimit}.");

        if (options is null)
            return Fail<TextSorterReport>(ToolErrorKind.Validation,
                TextSorterErrorCodes.EmptyInput, "Options are null.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var raw = input.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var inputLines = raw.Length;

            IEnumerable<string> working = raw;
            if (options.TrimLines)
                working = working.Select(l => l.Trim());

            int removedEmpty = 0;
            if (options.RemoveEmptyLines)
            {
                var before = working.Count();
                working = working.Where(l => !string.IsNullOrWhiteSpace(l));
                removedEmpty = before - working.Count();
            }

            var comparer = BuildComparer(options);

            int removedDuplicates = 0;
            if (options.RemoveDuplicates)
            {
                var list = working.ToList();
                var set = new HashSet<string>(list, comparer);
                removedDuplicates = list.Count - set.Count;
                working = set;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var sorted = options.Direction == SortDirection.Ascending
                ? working.OrderBy(l => l, comparer)
                : working.OrderByDescending(l => l, comparer);

            var output = string.Join(Environment.NewLine, sorted);
            sw.Stop();

            return Task.FromResult(ToolResult<TextSorterReport>.Ok(
                new TextSorterReport(output, inputLines, sorted.Count(),
                    removedEmpty, removedDuplicates, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<TextSorterReport>(ToolErrorKind.Cancelled,
                "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<TextSorterReport>(ToolErrorKind.Unexpected,
                TextSorterErrorCodes.Unexpected, ex.Message);
        }
    }

    /// <summary>
    /// StringComparer, hem IComparer&lt;string&gt; hem IEqualityComparer&lt;string&gt; uygular.
    /// Böylece hem sıralama hem HashSet yinelenen kaldırma için kullanılabilir.
    /// </summary>
    private static StringComparer BuildComparer(TextSorterOptions o)
    {
        if (o.IgnoreCase)
            return StringComparer.InvariantCultureIgnoreCase;

        return o.Mode == SortMode.Ordinal
            ? StringComparer.Ordinal
            : StringComparer.InvariantCulture;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}