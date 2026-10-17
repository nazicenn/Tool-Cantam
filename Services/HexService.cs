using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Hex kodlama/çözme işlemlerinin tek uygulaması.
/// <para>
/// Kodlarken metin UTF-8 baytlarına çevrilir, çıktı istenen biçim ve harf büyüklüğünde yazılır.
/// Çözerken yaygın ayırıcılar ve <c>0x</c>/<c>\x</c> önekleri kabul edilir; tek sayıda basamak,
/// geçersiz basamak veya geçersiz UTF-8 durumunda sessiz veri kaybı yerine hata döner.
/// </para>
/// </summary>
public sealed class HexService : IHexService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Encode(string? text, HexFormat format = HexFormat.Compact, bool uppercase = true)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                HexErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        // Eşleşmemiş surrogate sessizce U+FFFD'ye dönüşmemeli.
        if (TextValidation.TryFindUnpairedSurrogate(text, out int surrogateIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                HexErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {surrogateIndex}.");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var letters = uppercase ? HexDigits.Uppercase : HexDigits.Lowercase;

        string output = format switch
        {
            HexFormat.Spaced => Join(bytes, letters, ' '),
            HexFormat.ColonSeparated => Join(bytes, letters, ':'),
            _ => Join(bytes, letters, separator: null)
        };

        return ToolResult<string>.Ok(output);
    }

    public ToolResult<string> Decode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                HexErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var bytes = new List<byte>(text.Length / 2 + 1);
        int highNibble = -1;

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (HexDigits.IsSeparator(character))
                continue;

            // "0x" / "0X" öneki: '0' veri olarak sayılmaz, yoksa önek veriye karışırdı.
            if (character == '0' && index + 1 < text.Length && (text[index + 1] == 'x' || text[index + 1] == 'X'))
            {
                index++;
                continue;
            }

            // "\x" / "\X" öneki.
            if (character == '\\' && index + 1 < text.Length && (text[index + 1] == 'x' || text[index + 1] == 'X'))
            {
                index++;
                continue;
            }

            if (!HexDigits.TryParse(character, out int digit))
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    HexErrorCodes.InvalidDigit,
                    $"Unexpected character '{character}' (U+{(int)character:X4}) at index {index}.");
            }

            if (highNibble < 0)
            {
                highNibble = digit;
                continue;
            }

            bytes.Add((byte)((highNibble << 4) | digit));
            highNibble = -1;
        }

        // Tek basamak kaldıysa veri eksiktir; eskiden bu basamak sessizce atılıyordu.
        if (highNibble >= 0)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.InvalidFormat,
                HexErrorCodes.OddLength,
                "The hex payload contains an odd number of digits.");
        }

        if (bytes.Count > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                HexErrorCodes.OutputTooLarge,
                $"Decoded size {bytes.Count} bytes exceeds {MaxTextLengthLimit}.");
        }

        try
        {
            return ToolResult<string>.Ok(TextEncodings.StrictUtf8.GetString(bytes.ToArray()));
        }
        catch (DecoderFallbackException ex)
        {
            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, HexErrorCodes.NotUtf8, ex.Message);
        }
    }

    private static string Join(byte[] bytes, string letters, char? separator)
    {
        if (bytes.Length == 0)
            return string.Empty;

        int length = bytes.Length * 2 + (separator.HasValue ? bytes.Length - 1 : 0);
        var builder = new StringBuilder(length);

        for (int index = 0; index < bytes.Length; index++)
        {
            if (index > 0 && separator.HasValue)
                builder.Append(separator.Value);

            byte value = bytes[index];
            builder.Append(letters[value >> 4]).Append(letters[value & 0x0F]);
        }

        return builder.ToString();
    }
}
