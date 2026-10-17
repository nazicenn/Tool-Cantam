using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Metin istatistiklerinin tek uygulaması. Sayımlar tek geçişte yapılır; okuma/konuşma
/// süreleri standart kelime/dakika değerleriyle hesaplanır.
/// </summary>
public sealed class TextCounterService : ITextCounterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    /// <summary>Okuma hızı: 200 kelime/dakika.</summary>
    private const double ReadingWordsPerMinute = 200.0;

    /// <summary>Konuşma hızı: 130 kelime/dakika.</summary>
    private const double SpeakingWordsPerMinute = 130.0;

    /// <summary>En çok kullanılan kelime sayısı.</summary>
    private const int TopWordCount = 5;

    /// <summary>Çok kısa kelimeleri top listesinden çıkar (anlamsız).</summary>
    private const int MinWordLengthForTop = 3;

    private static readonly Regex WordRegex = new(@"\b\w+\b", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "are", "but", "not", "you", "all", "can", "her", "was", "one",
        "our", "out", "his", "has", "had", "him", "she", "its", "who", "did", "yes", "how",
        "why", "what", "when", "where", "this", "that", "with", "from", "have", "will",
        "your", "they", "them", "then", "than", "into", "only", "also", "just", "some",
        "such", "like", "over", "back", "after", "been", "would", "could", "should",
        "there", "their", "these", "those", "about", "which", "while", "were", "been",
        "bir", "ile", "için", "ve", "bu", "şu", "o", "da", "de", "mi", "mı", "mu", "mü"
    };

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<TextCounterReport> Count(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return ToolResult<TextCounterReport>.Ok(new TextCounterReport(
                0, 0, 0, 0, 0, 0,
                TimeSpan.Zero, TimeSpan.Zero,
                Array.Empty<WordFrequency>()));
        }

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<TextCounterReport>.Fail(
                ToolErrorKind.TooLarge,
                TextCounterErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit}.");
        }

        if (ContainsUnpairedSurrogate(text, out int badIndex))
        {
            return ToolResult<TextCounterReport>.Fail(
                ToolErrorKind.Validation,
                TextCounterErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {badIndex}.");
        }

        int charCount = text.Length;
        int charNoSpaces = text.Count(c => !char.IsWhiteSpace(c));
        int lineCount = text.Split('\n').Length;

        // Kelimeler
        var words = WordRegex.Matches(text);
        int wordCount = words.Count;

        // Cümleler
        int sentenceCount = CountSentences(text);

        // Paragraflar
        int paragraphCount = CountParagraphs(text);

        // Okuma / konuşma süreleri
        TimeSpan readingTime = TimeSpan.FromMinutes(wordCount / ReadingWordsPerMinute);
        TimeSpan speakingTime = TimeSpan.FromMinutes(wordCount / SpeakingWordsPerMinute);

        // En çok kullanılan kelimeler
        var topWords = ComputeTopWords(words);

        return ToolResult<TextCounterReport>.Ok(new TextCounterReport(
            charCount,
            charNoSpaces,
            wordCount,
            lineCount,
            paragraphCount,
            sentenceCount,
            readingTime,
            speakingTime,
            topWords));
    }

    private static int CountSentences(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (c is '.' or '!' or '?')
                count++;
        }
        return count > 0 ? count : (text.Trim().Length > 0 ? 1 : 0);
    }

    private static int CountParagraphs(string text)
    {
        var parts = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        int count = parts.Count(p => p.Trim().Length > 0);
        return count > 0 ? count : (text.Trim().Length > 0 ? 1 : 0);
    }

    private static List<WordFrequency> ComputeTopWords(MatchCollection words)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in words)
        {
            string word = match.Value.ToLowerInvariant();

            if (word.Length < MinWordLengthForTop) continue;
            if (StopWords.Contains(word)) continue;

            dict.TryGetValue(word, out int n);
            dict[word] = n + 1;
        }

        return dict
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(TopWordCount)
            .Select(kv => new WordFrequency(kv.Key, kv.Value))
            .ToList();
    }

    private static bool ContainsUnpairedSurrogate(string text, out int badIndex)
    {
        badIndex = -1;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    badIndex = i;
                    return true;
                }
                i++;
            }
            else if (char.IsLowSurrogate(c))
            {
                badIndex = i;
                return true;
            }
        }

        return false;
    }
}