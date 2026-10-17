using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Harf dönüşümünün tek uygulaması.
/// <para>
/// Tüm dönüşümler kültürden bağımsızdır (InvariantCulture) — aynı girdi her ortamda aynı sonucu verir.
/// Surrogate çiftleri korunur, geçersiz Unicode reddedilir.
/// </para>
/// </summary>
public sealed class CaseConverterService : ICaseConverterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public IReadOnlyList<CaseConversionMode> SupportedModes { get; } = new[]
    {
        CaseConversionMode.Upper,
        CaseConversionMode.Lower,
        CaseConversionMode.Title,
        CaseConversionMode.Sentence,
        CaseConversionMode.Camel,
        CaseConversionMode.Pascal,
        CaseConversionMode.Snake,
        CaseConversionMode.Kebab,
        CaseConversionMode.Alternating,
        CaseConversionMode.Invert
    };

    public ToolResult<string> Convert(string? text, CaseConversionMode mode)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                CaseConverterErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit}.");
        }

        if (ContainsUnpairedSurrogate(text, out int badIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                CaseConverterErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {badIndex}.");
        }

        try
        {
            string result = mode switch
            {
                CaseConversionMode.Upper => text.ToUpperInvariant(),
                CaseConversionMode.Lower => text.ToLowerInvariant(),
                CaseConversionMode.Title => ToTitle(text),
                CaseConversionMode.Sentence => ToSentence(text),
                CaseConversionMode.Camel => ToCamel(text),
                CaseConversionMode.Pascal => ToPascal(text),
                CaseConversionMode.Snake => ToSnake(text),
                CaseConversionMode.Kebab => ToKebab(text),
                CaseConversionMode.Alternating => ToAlternating(text),
                CaseConversionMode.Invert => ToInvert(text),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode.")
            };

            return ToolResult<string>.Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                CaseConverterErrorCodes.UnknownMode,
                ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Unexpected,
                CaseConverterErrorCodes.Unexpected,
                ex.Message);
        }
    }

    private static string ToTitle(string text)
    {
        var builder = new StringBuilder(text.Length);
        bool newWord = true;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c) || c == '-' || c == '_')
            {
                builder.Append(c);
                newWord = true;
            }
            else if (newWord)
            {
                builder.Append(char.ToUpperInvariant(c));
                newWord = false;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static string ToSentence(string text)
    {
        var builder = new StringBuilder(text.Length);
        bool startOfSentence = true;

        foreach (char c in text)
        {
            if (startOfSentence && char.IsLetter(c))
            {
                builder.Append(char.ToUpperInvariant(c));
                startOfSentence = false;
            }
            else
            {
                builder.Append(char.ToLowerInvariant(c));
                if (c is '.' or '!' or '?')
                    startOfSentence = true;
            }
        }

        return builder.ToString();
    }

    private static string ToCamel(string text)
    {
        var words = SplitWords(text);
        if (words.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        builder.Append(words[0].ToLowerInvariant());

        for (int i = 1; i < words.Count; i++)
        {
            string w = words[i].ToLowerInvariant();
            builder.Append(char.ToUpperInvariant(w[0]));
            if (w.Length > 1) builder.Append(w[1..]);
        }

        return builder.ToString();
    }

    private static string ToPascal(string text)
    {
        var words = SplitWords(text);
        var builder = new StringBuilder();

        foreach (var w in words)
        {
            string lower = w.ToLowerInvariant();
            builder.Append(char.ToUpperInvariant(lower[0]));
            if (lower.Length > 1) builder.Append(lower[1..]);
        }

        return builder.ToString();
    }

    private static string ToSnake(string text)
    {
        var words = SplitWords(text);
        return string.Join("_", words.Select(w => w.ToLowerInvariant()));
    }

    private static string ToKebab(string text)
    {
        var words = SplitWords(text);
        return string.Join("-", words.Select(w => w.ToLowerInvariant()));
    }

    private static string ToAlternating(string text)
    {
        var builder = new StringBuilder(text.Length);
        bool upper = true;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                builder.Append(upper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
                upper = !upper;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ToInvert(string text)
    {
        var builder = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                builder.Append(c).Append(text[i + 1]);
                i++;
                continue;
            }

            if (char.IsUpper(c))
                builder.Append(char.ToLowerInvariant(c));
            else if (char.IsLower(c))
                builder.Append(char.ToUpperInvariant(c));
            else
                builder.Append(c);
        }

        return builder.ToString();
    }

    private static List<string> SplitWords(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
            words.Add(current.ToString());

        return words;
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