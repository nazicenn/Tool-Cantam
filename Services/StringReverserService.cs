using System;
using System.Collections.Generic;
using System.Text;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Metin ters çevirme işleminin tek uygulaması.
/// <para>
/// Surrogate çiftleri korunur, geçersiz Unicode reddedilir. Tüm dönüşümler
/// tek geçişte yapılır; bellek kullanımı O(n)'dir.
/// </para>
/// </summary>
public sealed class StringReverserService : IStringReverserService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public IReadOnlyList<StringReverseMode> SupportedModes { get; } = new[]
    {
        StringReverseMode.ReverseAll,
        StringReverseMode.ReverseWords,
        StringReverseMode.ReverseEachWord,
        StringReverseMode.ReverseLines,
        StringReverseMode.ReverseLinesAndContent
    };

    public ToolResult<string> Reverse(string? text, StringReverseMode mode)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                StringReverserErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit}.");
        }

        if (ContainsUnpairedSurrogate(text, out int badIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                StringReverserErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {badIndex}.");
        }

        try
        {
            string result = mode switch
            {
                StringReverseMode.ReverseAll => ReverseAll(text),
                StringReverseMode.ReverseWords => ReverseWords(text),
                StringReverseMode.ReverseEachWord => ReverseEachWord(text),
                StringReverseMode.ReverseLines => ReverseLines(text),
                StringReverseMode.ReverseLinesAndContent => ReverseLinesAndContent(text),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode.")
            };

            return ToolResult<string>.Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                StringReverserErrorCodes.UnknownMode,
                ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Unexpected,
                StringReverserErrorCodes.Unexpected,
                ex.Message);
        }
    }

    private static string ReverseAll(string text)
    {
        // Surrogate çiftlerini koru
        var builder = new StringBuilder(text.Length);

        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];

            if (char.IsLowSurrogate(c) && i > 0 && char.IsHighSurrogate(text[i - 1]))
            {
                builder.Append(text[i - 1]).Append(c);
                i--;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ReverseWords(string text)
    {
        // Kelime sırasını ters çevir, boşlukları koru
        var words = SplitKeepingWhitespace(text);
        var builder = new StringBuilder(text.Length);

        for (int i = words.Count - 1; i >= 0; i--)
            builder.Append(words[i]);

        return builder.ToString();
    }

    private static string ReverseEachWord(string text)
    {
        var builder = new StringBuilder(text.Length);
        var word = new StringBuilder();

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (word.Length > 0)
                {
                    AppendReversed(builder, word.ToString());
                    word.Clear();
                }
                builder.Append(c);
            }
            else
            {
                word.Append(c);
            }
        }

        if (word.Length > 0)
            AppendReversed(builder, word.ToString());

        return builder.ToString();
    }

    private static void AppendReversed(StringBuilder builder, string word)
    {
        for (int i = word.Length - 1; i >= 0; i--)
        {
            char c = word[i];

            if (char.IsLowSurrogate(c) && i > 0 && char.IsHighSurrogate(word[i - 1]))
            {
                builder.Append(word[i - 1]).Append(c);
                i--;
            }
            else
            {
                builder.Append(c);
            }
        }
    }

    private static string ReverseLines(string text)
    {
        var lines = text.Split('\n');
        var builder = new StringBuilder(text.Length);

        for (int i = lines.Length - 1; i >= 0; i--)
        {
            builder.Append(lines[i]);
            if (i > 0) builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string ReverseLinesAndContent(string text)
    {
        var lines = text.Split('\n');
        var builder = new StringBuilder(text.Length);

        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i];
            string reversed = line.EndsWith('\r')
                ? ReverseAll(line[..^1]) + "\r"
                : ReverseAll(line);

            builder.Append(reversed);
            if (i > 0) builder.Append('\n');
        }

        return builder.ToString();
    }

    private static List<string> SplitKeepingWhitespace(string text)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inWhitespace = false;

        foreach (char c in text)
        {
            bool isWhitespace = char.IsWhiteSpace(c);

            if (current.Length == 0)
            {
                inWhitespace = isWhitespace;
                current.Append(c);
            }
            else if (isWhitespace == inWhitespace)
            {
                current.Append(c);
            }
            else
            {
                parts.Add(current.ToString());
                current.Clear();
                current.Append(c);
                inWhitespace = isWhitespace;
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts;
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