using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Text;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// URL yüzde kodlama/çözme işlemlerinin tek uygulaması.
/// <para>
/// Kurallar: yalnızca RFC 3986 "unreserved" karakterleri (<c>A-Z a-z 0-9 - . _ ~</c>) olduğu gibi kalır;
/// <see cref="UrlEncodingMode.FullUrl"/> modunda ayırıcı karakterler de korunur. Çözmede geçersiz
/// kaçış dizisi, bozuk UTF-8 veya eşleşmemiş surrogate sessizce değiştirilmez, hata döner.
/// </para>
/// </summary>
public sealed class UrlService : IUrlService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Encode(string? text, UrlEncodingMode mode = UrlEncodingMode.Component)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                UrlErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var builder = new StringBuilder(text.Length + (text.Length / 2));
        Span<byte> utf8 = stackalloc byte[4];

        for (int index = 0; index < text.Length;)
        {
            char current = text[index];

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.Validation,
                        UrlErrorCodes.InvalidUnicode,
                        $"Unpaired high surrogate (U+{(int)current:X4}) at index {index}.");
                }

                int pairBytes = Encoding.UTF8.GetBytes(text.AsSpan(index, 2), utf8);
                AppendPercentEncoded(builder, utf8[..pairBytes]);
                index += 2;
                continue;
            }

            if (char.IsLowSurrogate(current))
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.Validation,
                    UrlErrorCodes.InvalidUnicode,
                    $"Unpaired low surrogate (U+{(int)current:X4}) at index {index}.");
            }

            if (current < 0x80)
            {
                if (IsSafeCharacter(current, mode))
                {
                    builder.Append(current);
                }
                else if (mode == UrlEncodingMode.Form && current == ' ')
                {
                    builder.Append('+');
                }
                else
                {
                    builder.Append('%').Append(HexDigits.Uppercase[current >> 4]).Append(HexDigits.Uppercase[current & 0x0F]);
                }

                index++;
                continue;
            }

            // ASCII dışı karakterler UTF-8 baytlarına çevrilip tek tek kodlanır.
            int byteCount = Encoding.UTF8.GetBytes(text.AsSpan(index, 1), utf8);
            AppendPercentEncoded(builder, utf8[..byteCount]);
            index++;
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    public ToolResult<string> Decode(string? text, UrlEncodingMode mode = UrlEncodingMode.Component)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                UrlErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var bytes = new List<byte>(text.Length);
        Span<byte> utf8 = stackalloc byte[4];

        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];

            if (current == '%')
            {
                if (index + 2 >= text.Length ||
                    !HexDigits.TryParse(text[index + 1], out int high) ||
                    !HexDigits.TryParse(text[index + 2], out int low))
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.InvalidFormat,
                        UrlErrorCodes.InvalidEscape,
                        $"Invalid percent escape at index {index}.");
                }

                bytes.Add((byte)((high << 4) | low));
                index += 2;
                continue;
            }

            if (mode == UrlEncodingMode.Form && current == '+')
            {
                bytes.Add(0x20);
                continue;
            }

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    return ToolResult<string>.Fail(
                        ToolErrorKind.InvalidFormat,
                        UrlErrorCodes.InvalidUnicode,
                        $"Unpaired high surrogate (U+{(int)current:X4}) at index {index}.");
                }

                int pairBytes = Encoding.UTF8.GetBytes(text.AsSpan(index, 2), utf8);
                bytes.AddRange(utf8[..pairBytes].ToArray());
                index++;
                continue;
            }

            if (char.IsLowSurrogate(current))
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    UrlErrorCodes.InvalidUnicode,
                    $"Unpaired low surrogate (U+{(int)current:X4}) at index {index}.");
            }

            if (current < 0x80)
            {
                bytes.Add((byte)current);
                continue;
            }

            int byteCount = Encoding.UTF8.GetBytes(text.AsSpan(index, 1), utf8);
            bytes.AddRange(utf8[..byteCount].ToArray());
        }

        if (bytes.Count > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                UrlErrorCodes.OutputTooLarge,
                $"Decoded size {bytes.Count} bytes exceeds {MaxTextLengthLimit}.");
        }

        try
        {
            return ToolResult<string>.Ok(TextEncodings.StrictUtf8.GetString(bytes.ToArray()));
        }
        catch (DecoderFallbackException ex)
        {
            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, UrlErrorCodes.NotUtf8, ex.Message);
        }
    }

    /// <summary>
    /// Moda göre olduğu gibi bırakılabilecek ASCII karakterler.
    /// </summary>
    private static bool IsSafeCharacter(char character, UrlEncodingMode mode)
    {
        bool isUnreserved =
            (character >= 'A' && character <= 'Z') ||
            (character >= 'a' && character <= 'z') ||
            (character >= '0' && character <= '9') ||
            character is '-' or '.' or '_' or '~';

        if (isUnreserved)
            return true;

        if (mode != UrlEncodingMode.FullUrl)
            return false;

        // RFC 3986 gen-delims ve sub-delims: tam URL'de yapıyı korumak için kaçışlanmaz.
        return character switch
        {
            ':' or '/' or '?' or '#' or '[' or ']' or '@' => true,
            '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=' => true,
            _ => false
        };
    }

    private static void AppendPercentEncoded(StringBuilder builder, ReadOnlySpan<byte> bytes)
    {
        foreach (byte value in bytes)
        {
            builder.Append('%').Append(HexDigits.Uppercase[value >> 4]).Append(HexDigits.Uppercase[value & 0x0F]);
        }
    }
}
