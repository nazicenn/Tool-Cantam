using tool_cantam.Services.ErrorCodes;
using System.Globalization;
using System.Text;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// HTML kodlama/çözme işlemlerinin tek uygulaması.
/// <para>
/// Kodlama her zaman beş kritik karakteri kapsar (<c>&amp; &lt; &gt; " '</c>); tek tırnak sayısal
/// referansla yazılır çünkü <c>&amp;apos;</c> HTML4'te tanımlı değildir. Çözme; adlandırılmış
/// varlıkları, ondalık ve onaltılık sayısal referansları HTML5 kurallarına göre yorumlar.
/// </para>
/// </summary>
public sealed class HtmlEncoderService : IHtmlEncoderService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    /// <summary>Bir varlığın ad bölümü için üst sınır (HTML5 tarayıcıları da sınırlı uzunlukta arar).</summary>
    private const int MaxEntityBodyLength = 34;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Encode(string? text, bool useNamedEntities = true, bool encodeNonAscii = false)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                HtmlErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var builder = new StringBuilder(text.Length + (text.Length / 4));

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.Validation,
                        HtmlErrorCodes.InvalidUnicode,
                        $"Unpaired high surrogate (U+{(int)character:X4}) at index {index}.");
                }

                if (encodeNonAscii)
                    AppendNumericReference(builder, char.ConvertToUtf32(character, text[index + 1]));
                else
                    builder.Append(character).Append(text[index + 1]);

                index++;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.Validation,
                    HtmlErrorCodes.InvalidUnicode,
                    $"Unpaired low surrogate (U+{(int)character:X4}) at index {index}.");
            }

            string? entityName = HtmlEntities.GetEntityName(character);
            if (entityName is not null)
            {
                if (useNamedEntities)
                    builder.Append('&').Append(entityName).Append(';');
                else
                    AppendNumericReference(builder, character);

                continue;
            }

            // &apos; HTML4'te bulunmadığı için tek tırnak her zaman sayısal yazılır.
            if (character == '\'')
            {
                AppendNumericReference(builder, character);
                continue;
            }

            if (encodeNonAscii && character > 0x7F)
            {
                AppendNumericReference(builder, character);
                continue;
            }

            builder.Append(character);
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    public ToolResult<string> Decode(string? text, bool strictEntities = true)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                HtmlErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var builder = new StringBuilder(text.Length);

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (character != '&')
            {
                builder.Append(character);
                continue;
            }

            int semicolon = text.IndexOf(';', index + 1);

            if (semicolon < 0 || semicolon - index > MaxEntityBodyLength)
            {
                if (strictEntities)
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.InvalidFormat,
                        HtmlErrorCodes.MissingSemicolon,
                        $"Entity starting at index {index} is not terminated with ';'.");
                }

                builder.Append('&');
                continue;
            }

            string body = text.Substring(index + 1, semicolon - index - 1);

            if (body.Length == 0)
            {
                if (strictEntities)
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.InvalidFormat,
                        HtmlErrorCodes.InvalidEntity,
                        $"Empty entity at index {index}.");
                }

                builder.Append('&');
                continue;
            }

            if (body[0] == '#')
            {
                if (!TryResolveNumericReference(body, out int codePoint, out string? failureCode, out string? failureDetail))
                {
                    if (strictEntities)
                        return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, failureCode!, failureDetail);

                    builder.Append('&');
                    continue;
                }

                builder.Append(char.ConvertFromUtf32(codePoint));
                index = semicolon;
                continue;
            }

            if (HtmlEntities.TryGetCharacter(body, out string value))
            {
                builder.Append(value);
                index = semicolon;
                continue;
            }

            if (strictEntities)
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    HtmlErrorCodes.UnknownEntity,
                    $"Unknown HTML entity '&{body};' at index {index}.");
            }

            builder.Append('&');
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    private static void AppendNumericReference(StringBuilder builder, int codePoint) =>
        builder.Append("&#").Append(codePoint.ToString(CultureInfo.InvariantCulture)).Append(';');

    /// <summary>
    /// <c>#123</c> veya <c>#x1F600</c> gövdesini kod noktasına çevirir ve geçerliliğini doğrular.
    /// </summary>
    private static bool TryResolveNumericReference(string body, out int codePoint, out string? failureCode, out string? failureDetail)
    {
        codePoint = 0;
        failureCode = null;
        failureDetail = null;

        if (body.Length < 2)
        {
            failureCode = HtmlErrorCodes.InvalidEntity;
            failureDetail = $"Numeric reference '{body}' has no digits.";
            return false;
        }

        bool isHexadecimal = body[1] == 'x' || body[1] == 'X';
        string digits = isHexadecimal ? body[2..] : body[1..];

        if (digits.Length == 0)
        {
            failureCode = HtmlErrorCodes.InvalidEntity;
            failureDetail = $"Numeric reference '{body}' has no digits.";
            return false;
        }

        foreach (char digit in digits)
        {
            if (isHexadecimal)
            {
                if (!HexDigits.TryParse(digit, out _))
                {
                    failureCode = HtmlErrorCodes.InvalidEntity;
                    failureDetail = $"Numeric reference '{body}' contains a non-hexadecimal digit.";
                    return false;
                }
            }
            else if (digit is < '0' or > '9')
            {
                failureCode = HtmlErrorCodes.InvalidEntity;
                failureDetail = $"Numeric reference '{body}' contains a non-decimal digit.";
                return false;
            }
        }

        NumberStyles style = isHexadecimal ? NumberStyles.HexNumber : NumberStyles.None;
        if (!int.TryParse(digits, style, CultureInfo.InvariantCulture, out int parsed))
        {
            failureCode = HtmlErrorCodes.InvalidCodePoint;
            failureDetail = $"Numeric reference '{body}' is out of range.";
            return false;
        }

        // HTML5, eski Windows-1252 aralığındaki referansları kendi karakterlerine eşler.
        parsed = HtmlEntities.RemapLegacyCodePoint(parsed);

        if (parsed == 0 || parsed > 0x10FFFF || (parsed >= 0xD800 && parsed <= 0xDFFF))
        {
            failureCode = HtmlErrorCodes.InvalidCodePoint;
            failureDetail = $"Numeric reference '{body}' resolves to an invalid code point (U+{parsed:X4}).";
            return false;
        }

        codePoint = parsed;
        return true;
    }
}
