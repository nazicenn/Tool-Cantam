using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Metinden URL-friendly slug üretir.
/// Türkçe karakterler + Unicode NFD + diacritics temizliği + isteğe bağlı ASCII zorlaması.
/// </summary>
public sealed class SlugGeneratorService : ISlugGeneratorService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public const int MaxSlugLengthLimit = 200;
    private const int MaxLengthUnlimited = 0;

    // Türkçe + yaygın diacritics (NFD ile çözülemeyenler)
    private static readonly (char From, char To)[] TurkishMap =
    {
        ('ç', 'c'), ('Ç', 'C'),
        ('ğ', 'g'), ('Ğ', 'G'),
        ('ı', 'i'), ('I', 'I'),
        ('ö', 'o'), ('Ö', 'O'),
        ('ş', 's'), ('Ş', 'S'),
        ('ü', 'u'), ('Ü', 'U'),
        ('ß', 's'),
        ('æ', 'a'), ('Æ', 'A'),
        ('ø', 'o'), ('Ø', 'O'),
        ('œ', 'o'), ('Œ', 'O'),
    };

    public int MaxInputLength => MaxInputLengthLimit;
    public int MaxSlugLength => MaxSlugLengthLimit;

    public Task<ToolResult<SlugReport>> GenerateAsync(
        string? input, SlugOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Fail<SlugReport>(ToolErrorKind.Validation,
                SlugGeneratorErrorCodes.EmptyInput, "Input is empty.");

        if (input.Length > MaxInputLengthLimit)
            return Fail<SlugReport>(ToolErrorKind.TooLarge,
                SlugGeneratorErrorCodes.InputTooLarge,
                $"Input length {input.Length} exceeds {MaxInputLengthLimit}.");

        if (options is null)
            return Fail<SlugReport>(ToolErrorKind.Validation,
                SlugGeneratorErrorCodes.EmptyInput, "Options are null.");

        if (options.Separator == '\0' || char.IsLetterOrDigit(options.Separator))
            return Fail<SlugReport>(ToolErrorKind.Validation,
                SlugGeneratorErrorCodes.InvalidSeparator,
                $"Separator '{options.Separator}' must be a punctuation char.");

        if (options.MaxLength < 0 || options.MaxLength > MaxSlugLengthLimit)
            return Fail<SlugReport>(ToolErrorKind.Validation,
                SlugGeneratorErrorCodes.InvalidMaxLength,
                $"MaxLength must be 0..{MaxSlugLengthLimit} (got {options.MaxLength}).");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var slug = BuildSlug(input, options, cancellationToken);

            if (string.IsNullOrEmpty(slug))
                return Fail<SlugReport>(ToolErrorKind.Validation,
                    SlugGeneratorErrorCodes.EmptyResult,
                    "Slug is empty after normalization.");

            sw.Stop();
            return Task.FromResult(ToolResult<SlugReport>.Ok(
                new SlugReport(slug, input.Length, slug.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<SlugReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<SlugReport>(ToolErrorKind.Unexpected,
                SlugGeneratorErrorCodes.Unexpected, ex.Message);
        }
    }

    private static string BuildSlug(string input, SlugOptions options, CancellationToken ct)
    {
        var sb = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            ct.ThrowIfCancellationRequested();

            if (c == options.Separator || char.IsWhiteSpace(c) || c == '-' || c == '_' || c == '.')
            {
                AppendSeparator(sb, options.Separator);
                continue;
            }

            if (!options.AllowUnicode)
            {
                var mapped = NormalizeAscii(c);
                if (mapped is char ch)
                {
                    if (char.IsLetterOrDigit(ch))
                    {
                        sb.Append(options.ToLowercase ? char.ToLowerInvariant(ch) : ch);
                    }
                    else
                    {
                        AppendSeparator(sb, options.Separator);
                    }
                    continue;
                }
                // ASCII'ye dönüşemeyen karakter → ayırıcı
                AppendSeparator(sb, options.Separator);
            }
            else
            {
                // Unicode korunur — harf/rakam dışındakiler ayırıcıya döner
                if (char.IsLetterOrDigit(c) || IsCombiningMark(c))
                {
                    sb.Append(options.ToLowercase ? char.ToLowerInvariant(c) : c);
                }
                else
                {
                    AppendSeparator(sb, options.Separator);
                }
            }
        }

        // Baştaki/sondaki separator'ları temizle
        var result = sb.ToString().Trim(options.Separator);
        result = CollapseSeparators(result, options.Separator);

        if (options.MaxLength > 0 && result.Length > options.MaxLength)
            result = result[..options.MaxLength].TrimEnd(options.Separator);

        return result;
    }

    private static void AppendSeparator(StringBuilder sb, char sep)
    {
        if (sb.Length > 0 && sb[^1] != sep)
            sb.Append(sep);
    }

    private static string CollapseSeparators(string s, char sep)
    {
        var sb = new StringBuilder(s.Length);
        char? prev = null;
        foreach (var c in s)
        {
            if (c == sep && prev == sep) continue;
            sb.Append(c);
            prev = c;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Tek karakteri ASCII karşılığına çevirir. Türkçe map + Unicode NFD → diacritics temizliği.
    /// Dönüştürülemiyorsa null.
    /// </summary>
    private static char? NormalizeAscii(char c)
    {
        // Türkçe özel karakterler
        foreach (var (from, to) in TurkishMap)
            if (c == from) return to;

        // ASCII zaten
        if (c < 128) return c;

        // Unicode NFD → combining marks at, base char al
        var s = c.ToString().Normalize(NormalizationForm.FormD);
        var baseChar = s[0];
        if (baseChar < 128) return baseChar;

        // Fallback: UnicodeCategory kontrolü
        var cat = CharUnicodeInfo.GetUnicodeCategory(c);
        if (cat == UnicodeCategory.NonSpacingMark) return null;

        return null;
    }

    private static bool IsCombiningMark(char c)
        => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}