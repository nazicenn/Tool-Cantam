using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Kelime/karakter/cümle/paragraf + okuma-konuşma süresi + top kelimeler.</summary>
public sealed class WordCounterService : IWordCounterService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the","and","for","are","but","not","you","all","can","her","was","one","our","out",
        "his","has","had","him","she","its","who","did","yes","how","why","what",
        "when","where","this","that","with","from","have","will","your","they","them","then",
        "than","into","only","also","just","some","such","like","over","back","after","been",
        "would","could","should","there","their","these","those","about","which","while",
        "bir","ve","bu","da","de","için","ile","ama","veya","çok","daha","gibi","kadar",
        "ancak","çünkü","eğer","ise","değil","var","yok",
    };

    public Task<ToolResult<WordCountReport>> AnalyzeAsync(string? input, CancellationToken ct)
    {
        input ??= string.Empty;
        if (input.Length > MaxInputLengthLimit)
            return Fail<WordCountReport>(ToolErrorKind.TooLarge, WordCounterErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            int chars = input.Length;
            int charsNoSpaces = input.Count(c => !char.IsWhiteSpace(c));

            var words = string.IsNullOrWhiteSpace(input)
                ? Array.Empty<string>()
                : Regex.Split(input.Trim(), @"\s+", RegexOptions.None).Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();
            int wordCount = words.Length;

            int sentences = Regex.Matches(input, @"[.!?]+").Count;
            if (sentences == 0 && wordCount > 0) sentences = 1;

            int paragraphs = input.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Count(p => !string.IsNullOrWhiteSpace(p));
            if (paragraphs == 0 && wordCount > 0) paragraphs = 1;

            int lines = string.IsNullOrEmpty(input) ? 0 : input.Split('\n').Length;

            var reading = TimeSpan.FromMinutes(wordCount / 200.0);
            var speaking = TimeSpan.FromMinutes(wordCount / 130.0);

            var topWords = words
                .Select(w => w.ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')'))
                .Where(w => w.Length > 2 && !StopWords.Contains(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new WordCounterWordFreq(g.Key, g.Count()))
                .ToList();

            sw.Stop();
            return Task.FromResult(ToolResult<WordCountReport>.Ok(new WordCountReport(
                chars, charsNoSpaces, wordCount, sentences, paragraphs, lines,
                reading, speaking, topWords, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<WordCountReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<WordCountReport>(ToolErrorKind.Unexpected, WordCounterErrorCodes.Unexpected, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}