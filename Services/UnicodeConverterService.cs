using System.Globalization;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Unicode kaçış dönüşümlerinin tek uygulaması.
/// <para>
/// Kodlama kod noktaları üzerinde çalışır; bu yüzden BMP dışı karakterler <c>\uXXXX</c> biçiminde
/// doğru bir vekil çifti olarak, <c>\UXXXXXXXX</c> biçiminde ise tek kaçış olarak yazılır.
/// Çözme katmanı düzenli ifade kullanmaz (zaman aşımı riski yoktur) ve bozuk girdiyi sessizce
/// geçirmez: eşleşmemiş vekil, geçersiz kod noktası ve bozuk UTF-8 bayt dizisi hata döner.
/// </para>
/// </summary>
public sealed class UnicodeConverterService : IUnicodeConverterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    private const int MaxCodePoint = 0x10FFFF;
    private const int HighSurrogateStart = 0xD800;
    private const int HighSurrogateEnd = 0xDBFF;
    private const int LowSurrogateStart = 0xDC00;
    private const int LowSurrogateEnd = 0xDFFF;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Escape(string? text, UnicodeEscapeFormat format = UnicodeEscapeFormat.Utf16Escape)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                UnicodeErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        if (TextValidation.TryFindUnpairedSurrogate(text, out int surrogateIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                UnicodeErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {surrogateIndex}.");
        }

        var builder = new StringBuilder(text.Length * 6);
        Span<byte> utf8 = stackalloc byte[4];

        foreach (Rune rune in text.EnumerateRunes())
        {
            switch (format)
            {
                case UnicodeEscapeFormat.CodePointEscape:
                    AppendHex(builder, "\\U", rune.Value, 8);
                    break;

                case UnicodeEscapeFormat.Utf8ByteEscape:
                    int byteCount = rune.EncodeToUtf8(utf8);
                    for (int index = 0; index < byteCount; index++)
                        AppendHex(builder, "\\x", utf8[index], 2);
                    break;

                case UnicodeEscapeFormat.CodePointNotation:
                    AppendHex(builder, "U+", rune.Value, 4);
                    break;

                default:
                    if (rune.Value <= 0xFFFF)
                    {
                        AppendHex(builder, "\\u", rune.Value, 4);
                    }
                    else
                    {
                        // BMP dışı: doğru vekil çifti yazılır.
                        int offset = rune.Value - 0x10000;
                        AppendHex(builder, "\\u", HighSurrogateStart + (offset >> 10), 4);
                        AppendHex(builder, "\\u", LowSurrogateStart + (offset & 0x3FF), 4);
                    }

                    break;
            }

            if (builder.Length > MaxTextLengthLimit)
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.TooLarge,
                    UnicodeErrorCodes.OutputTooLarge,
                    $"The escaped result exceeds {MaxTextLengthLimit} characters.");
            }
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    public ToolResult<string> Unescape(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                UnicodeErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var builder = new StringBuilder(text.Length);

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            // Kaçış işareti olmadan yazılan gösterim: U+20AC
            if ((character == 'U' || character == 'u') && index + 2 < text.Length && text[index + 1] == '+')
            {
                if (TryReadHex(text, index + 2, 6, out int notationValue, out int notationDigits) && notationDigits > 0)
                {
                    if (!IsValidCodePoint(notationValue, out string? notationDetail))
                        return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, UnicodeErrorCodes.InvalidCodePoint, notationDetail);

                    builder.Append(char.ConvertFromUtf32(notationValue));
                    index += 1 + notationDigits;
                    continue;
                }

                builder.Append(character);
                continue;
            }

            if (character != '\\')
            {
                builder.Append(character);
                continue;
            }

            if (index + 1 >= text.Length)
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    UnicodeErrorCodes.InvalidEscape,
                    $"Trailing backslash at index {index}.");
            }

            char kind = text[index + 1];

            switch (kind)
            {
                case 'u':
                    {
                        int digitsStart = index + 2;

                        // \u{1F600} biçimi
                        if (digitsStart < text.Length && text[digitsStart] == '{')
                        {
                            int closing = text.IndexOf('}', digitsStart + 1);

                            if (closing < 0)
                            {
                                return ToolResult<string>.Fail(
                                    ToolErrorKind.InvalidFormat,
                                    UnicodeErrorCodes.InvalidEscape,
                                    $"Unterminated '\\u{{' escape at index {index}.");
                            }

                            string hex = text[(digitsStart + 1)..closing];

                            if (hex.Length is 0 or > 6 || !IsAllHexDigits(hex) ||
                                !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int bracedValue))
                            {
                                return ToolResult<string>.Fail(
                                    ToolErrorKind.InvalidFormat,
                                    UnicodeErrorCodes.InvalidEscape,
                                    $"Invalid '\\u{{{hex}}}' escape at index {index}.");
                            }

                            if (!IsValidCodePoint(bracedValue, out string? bracedDetail))
                                return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, UnicodeErrorCodes.InvalidCodePoint, bracedDetail);

                            builder.Append(char.ConvertFromUtf32(bracedValue));
                            index = closing;
                            continue;
                        }

                        if (!TryReadFixedHex(text, digitsStart, 4, out int unit))
                        {
                            return ToolResult<string>.Fail(
                                ToolErrorKind.InvalidFormat,
                                UnicodeErrorCodes.InvalidEscape,
                                $"Expected four hexadecimal digits after '\\u' at index {index}.");
                        }

                        if (unit is >= HighSurrogateStart and <= HighSurrogateEnd)
                        {
                            int nextEscape = digitsStart + 4;
                            int low = 0;

                            bool hasPair =
                                nextEscape + 5 < text.Length &&
                                text[nextEscape] == '\\' &&
                                text[nextEscape + 1] == 'u' &&
                                TryReadFixedHex(text, nextEscape + 2, 4, out low) &&
                                low is >= LowSurrogateStart and <= LowSurrogateEnd;

                            if (!hasPair)
                            {
                                return ToolResult<string>.Fail(
                                    ToolErrorKind.InvalidFormat,
                                    UnicodeErrorCodes.LoneSurrogate,
                                    $"High surrogate U+{unit:X4} at index {index} is not followed by a low surrogate escape.");
                            }

                            builder.Append(char.ConvertFromUtf32(0x10000 + ((unit - HighSurrogateStart) << 10) + (low - LowSurrogateStart)));
                            index = nextEscape + 5;
                            continue;
                        }

                        if (unit is >= LowSurrogateStart and <= LowSurrogateEnd)
                        {
                            return ToolResult<string>.Fail(
                                ToolErrorKind.InvalidFormat,
                                UnicodeErrorCodes.LoneSurrogate,
                                $"Low surrogate U+{unit:X4} at index {index} has no preceding high surrogate.");
                        }

                        builder.Append((char)unit);
                        index = digitsStart + 3;
                        continue;
                    }

                case 'U':
                    {
                        if (!TryReadFixedHex(text, index + 2, 8, out int codePoint))
                        {
                            return ToolResult<string>.Fail(
                                ToolErrorKind.InvalidFormat,
                                UnicodeErrorCodes.InvalidEscape,
                                $"Expected eight hexadecimal digits after '\\U' at index {index}.");
                        }

                        if (!IsValidCodePoint(codePoint, out string? detail))
                            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, UnicodeErrorCodes.InvalidCodePoint, detail);

                        builder.Append(char.ConvertFromUtf32(codePoint));
                        index += 9;
                        continue;
                    }

                case 'x':
                    {
                        var bytes = new List<byte>(4);
                        int cursor = index;

                        while (cursor + 3 < text.Length &&
                               text[cursor] == '\\' &&
                               text[cursor + 1] == 'x' &&
                               TryReadFixedHex(text, cursor + 2, 2, out int byteValue))
                        {
                            bytes.Add((byte)byteValue);
                            cursor += 4;
                        }

                        if (bytes.Count == 0)
                        {
                            return ToolResult<string>.Fail(
                                ToolErrorKind.InvalidFormat,
                                UnicodeErrorCodes.InvalidEscape,
                                $"Expected two hexadecimal digits after '\\x' at index {index}.");
                        }

                        try
                        {
                            builder.Append(TextEncodings.StrictUtf8.GetString(bytes.ToArray()));
                        }
                        catch (DecoderFallbackException ex)
                        {
                            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, UnicodeErrorCodes.NotUtf8, ex.Message);
                        }

                        index = cursor - 1;
                        continue;
                    }

                case 'n': builder.Append('\n'); index++; continue;
                case 'r': builder.Append('\r'); index++; continue;
                case 't': builder.Append('\t'); index++; continue;
                case '0': builder.Append('\0'); index++; continue;
                case 'a': builder.Append('\a'); index++; continue;
                case 'b': builder.Append('\b'); index++; continue;
                case 'f': builder.Append('\f'); index++; continue;
                case 'v': builder.Append('\v'); index++; continue;
                case '\\': builder.Append('\\'); index++; continue;
                case '\'': builder.Append('\''); index++; continue;
                case '"': builder.Append('"'); index++; continue;

                default:
                    return ToolResult<string>.Fail(
                        ToolErrorKind.InvalidFormat,
                        UnicodeErrorCodes.InvalidEscape,
                        $"Unknown escape '\\{kind}' at index {index}.");
            }
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    private static void AppendHex(StringBuilder builder, string prefix, int value, int digits) =>
        builder.Append(prefix).Append(value.ToString("X" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));

    private static bool IsValidCodePoint(int value, out string? detail)
    {
        if (value < 0 || value > MaxCodePoint || (value >= HighSurrogateStart && value <= LowSurrogateEnd))
        {
            detail = $"U+{value:X4} is not a valid Unicode code point.";
            return false;
        }

        detail = null;
        return true;
    }

    private static bool IsAllHexDigits(string value)
    {
        foreach (char character in value)
        {
            if (!HexDigits.TryParse(character, out _))
                return false;
        }

        return value.Length > 0;
    }

    private static bool TryReadFixedHex(string text, int start, int count, out int value)
    {
        value = 0;

        if (start + count > text.Length)
            return false;

        for (int offset = 0; offset < count; offset++)
        {
            if (!HexDigits.TryParse(text[start + offset], out int digit))
                return false;

            value = (value << 4) | digit;
        }

        return true;
    }

    /// <summary>En fazla <paramref name="maxDigits"/> basamak okur; kaç basamak okunduğunu bildirir.</summary>
    private static bool TryReadHex(string text, int start, int maxDigits, out int value, out int digitsRead)
    {
        value = 0;
        digitsRead = 0;

        while (digitsRead < maxDigits && start + digitsRead < text.Length)
        {
            if (!HexDigits.TryParse(text[start + digitsRead], out int digit))
                break;

            value = (value << 4) | digit;
            digitsRead++;
        }

        return digitsRead > 0;
    }
}
