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
/// Satır bazlı LCS diff. Değişen satırlar Changed, eklenen Added, silinen Removed olarak işaretlenir.
/// </summary>
public sealed class TextDiffService : ITextDiffService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;

    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<DiffReport>> CompareAsync(
        string? left, string? right, DiffOptions options, CancellationToken cancellationToken)
    {
        left ??= string.Empty;
        right ??= string.Empty;

        if (left.Length == 0 && right.Length == 0)
            return Fail<DiffReport>(ToolErrorKind.Validation,
                TextDiffErrorCodes.BothEmpty, "Both inputs are empty.");

        if (left.Length > MaxInputLengthLimit || right.Length > MaxInputLengthLimit)
            return Fail<DiffReport>(ToolErrorKind.TooLarge,
                TextDiffErrorCodes.InputTooLarge, "Input exceeds 10 MB.");

        if (options is null)
            return Fail<DiffReport>(ToolErrorKind.Validation,
                TextDiffErrorCodes.BothEmpty, "Options are null.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var leftLines = SplitLines(left, options);
            var rightLines = SplitLines(right, options);

            var lines = DiffLcs(leftLines, rightLines, options, cancellationToken);

            int added = lines.Count(l => l.Kind == DiffLineKind.Added);
            int removed = lines.Count(l => l.Kind == DiffLineKind.Removed);
            int changed = lines.Count(l => l.Kind == DiffLineKind.Changed);
            int unchanged = lines.Count(l => l.Kind == DiffLineKind.Unchanged);

            sw.Stop();
            return Task.FromResult(ToolResult<DiffReport>.Ok(
                new DiffReport(lines, added, removed, changed, unchanged, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<DiffReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<DiffReport>(ToolErrorKind.Unexpected, TextDiffErrorCodes.Unexpected, ex.Message);
        }
    }

    private static List<string> SplitLines(string s, DiffOptions o)
    {
        var raw = s.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var result = new List<string>(raw.Length);
        foreach (var line in raw)
        {
            if (o.IgnoreEmptyLines && string.IsNullOrWhiteSpace(line)) continue;
            result.Add(line);
        }
        return result;
    }

    private static string Normalize(string line, DiffOptions o)
    {
        if (o.IgnoreWhitespace)
            line = System.Text.RegularExpressions.Regex.Replace(line, @"\s+", " ").Trim();
        if (o.IgnoreCase)
            line = line.ToLowerInvariant();
        return line;
    }

    private static List<DiffLine> DiffLcs(
        List<string> left, List<string> right, DiffOptions o, CancellationToken ct)
    {
        int n = left.Count, m = right.Count;

        // LCS length table
        var lcs = new int[n + 1, m + 1];
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                var a = Normalize(left[i - 1], o);
                var b = Normalize(right[j - 1], o);
                lcs[i, j] = a == b
                    ? lcs[i - 1, j - 1] + 1
                    : Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
            }
            ct.ThrowIfCancellationRequested();
        }

        // Backtrack
        var result = new List<DiffLine>(n + m);
        int x = n, y = m;
        while (x > 0 || y > 0)
        {
            ct.ThrowIfCancellationRequested();

            if (x > 0 && y > 0)
            {
                var a = Normalize(left[x - 1], o);
                var b = Normalize(right[y - 1], o);
                if (a == b)
                {
                    result.Add(new DiffLine(DiffLineKind.Unchanged, left[x - 1], right[y - 1], x - 1, y - 1));
                    x--; y--;
                    continue;
                }
            }

            if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y]))
            {
                result.Add(new DiffLine(DiffLineKind.Added, null, right[y - 1], -1, y - 1));
                y--;
            }
            else
            {
                result.Add(new DiffLine(DiffLineKind.Removed, left[x - 1], null, x - 1, -1));
                x--;
            }
        }

        result.Reverse();

        // Ardışık Removed + Added çiftlerini Changed olarak birleştir
        var merged = new List<DiffLine>(result.Count);
        int k = 0;
        while (k < result.Count)
        {
            if (k + 1 < result.Count &&
                result[k].Kind == DiffLineKind.Removed &&
                result[k + 1].Kind == DiffLineKind.Added)
            {
                merged.Add(new DiffLine(DiffLineKind.Changed,
                    result[k].Left, result[k + 1].Right,
                    result[k].LeftIndex, result[k + 1].RightIndex));
                k += 2;
            }
            else
            {
                merged.Add(result[k]);
                k++;
            }
        }

        return merged;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}