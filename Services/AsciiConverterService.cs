using System.Globalization;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Metin ile Unicode kod noktaları arasındaki dönüşümün tek uygulaması.
/// <para>
/// Kodlarken metin, UTF-16 kod birimleri yerine <b>kod noktaları</b> üzerinden taranır; böylece
/// emoji ve diğer ASCII dışı karakterler tek bir değer olarak temsil edilir. Çözerken her değer
/// geçerli bir Unicode kod noktası olmalıdır: 0 ile 0x10FFFF arasında ve vekil (surrogate) aralığın
/// dışında. Geçersiz değerler sessizce bozuk metne dönüşmez.
/// </para>
/// </summary>
public sealed class AsciiConverterService : IAsciiConverterService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    private static readonly char[] TokenSeparators = { ' ', '\t', '\r', '\n', ',', ';', '|' };

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> ToCodes(string? text, CharacterCodeFormat format = CharacterCodeFormat.Decimal)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                CharacterCodeErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        if (TextValidation.TryFindUnpairedSurrogate(text, out int surrogateIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                CharacterCodeErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {surrogateIndex}.");
        }

        var builder = new StringBuilder(text.Length * (format == CharacterCodeFormat.Hexadecimal ? 8 : 5));
        bool isFirst = true;

        // EnumerateRunes, vekil çiftleri tek bir kod noktasına indirger.
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (!isFirst)
                builder.Append(' ');

            isFirst = false;

            if (format == CharacterCodeFormat.Hexadecimal)
                builder.Append("U+").Append(rune.Value.ToString("X4", CultureInfo.InvariantCulture));
            else
                builder.Append(rune.Value.ToString(CultureInfo.InvariantCulture));

            if (builder.Length > MaxTextLengthLimit)
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.TooLarge,
                    CharacterCodeErrorCodes.OutputTooLarge,
                    $"The code list exceeds {MaxTextLengthLimit} characters.");
            }
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    public ToolResult<string> ToText(string? codes, CharacterCodeFormat defaultFormat = CharacterCodeFormat.Decimal)
    {
        if (string.IsNullOrWhiteSpace(codes))
            return ToolResult<string>.Ok(string.Empty);

        if (codes.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                CharacterCodeErrorCodes.InputTooLarge,
                $"Input length {codes.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        string[] tokens = codes.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        var builder = new StringBuilder(tokens.Length);

        foreach (string token in tokens)
        {
            if (!TryParseToken(token, defaultFormat, out int codePoint, out string? failureCode, out string? failureDetail))
                return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, failureCode!, failureDetail);

            builder.Append(char.ConvertFromUtf32(codePoint));
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    private static bool TryParseToken(
        string token,
        CharacterCodeFormat defaultFormat,
        out int codePoint,
        out string? failureCode,
        out string? failureDetail)
    {
        codePoint = 0;
        failureCode = null;
        failureDetail = null;

        string body = token;
        bool hasHexPrefix = false;

        if (body.StartsWith("U+", StringComparison.OrdinalIgnoreCase) ||
            body.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            body.StartsWith("\\u", StringComparison.Ordinal) ||
            body.StartsWith("\\x", StringComparison.Ordinal))
        {
            hasHexPrefix = true;
            body = body[2..];
        }

        if (body.Length == 0)
        {
            failureCode = CharacterCodeErrorCodes.InvalidToken;
            failureDetail = $"Token '{token}' does not contain a value.";
            return false;
        }

        bool isHexadecimal = hasHexPrefix || defaultFormat == CharacterCodeFormat.Hexadecimal;
        NumberStyles style = isHexadecimal ? NumberStyles.HexNumber : NumberStyles.None;

        foreach (char character in body)
        {
            bool isValidDigit = isHexadecimal
                ? HexDigits.TryParse(character, out _)
                : character is >= '0' and <= '9';

            if (!isValidDigit)
            {
                failureCode = CharacterCodeErrorCodes.InvalidToken;
                failureDetail = $"Token '{token}' is not a valid {(isHexadecimal ? "hexadecimal" : "decimal")} number.";
                return false;
            }
        }

        if (!int.TryParse(body, style, CultureInfo.InvariantCulture, out int value))
        {
            failureCode = CharacterCodeErrorCodes.InvalidCodePoint;
            failureDetail = $"Token '{token}' is out of range.";
            return false;
        }

        // Vekil aralık geçerli bir kod noktası değildir; eskiden (char) dönüşümü bozuk metin üretiyordu.
        if (value < 0 || value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
        {
            failureCode = CharacterCodeErrorCodes.InvalidCodePoint;
            failureDetail = $"Token '{token}' resolves to an invalid Unicode code point (U+{value:X4}).";
            return false;
        }

        codePoint = value;
        return true;
    }
}
