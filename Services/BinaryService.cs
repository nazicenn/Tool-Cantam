using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// İkili (binary) kodlama/çözme işlemlerinin tek uygulaması.
/// <para>
/// Kodlarken her UTF-8 baytı 8 bit olarak yazılır. Çözerken boşluk, alt çizgi ve <c>0b</c> önekleri
/// yok sayılır; bit sayısı 8'in katı değilse bu bir hata olarak bildirilir (eskiden artan bitler
/// sessizce atılıyordu). Çözülen baytlar geçerli UTF-8 değilse bozuk metin üretilmez.
/// </para>
/// </summary>
public sealed class BinaryService : IBinaryService
{
    /// <summary>İkili gösterim için karakter sınırı. Aynı sınır kodlama çıktısı için de geçerlidir.</summary>
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Encode(string? text, BinaryFormat format = BinaryFormat.Compact, bool includeBytePrefix = false)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                BinaryErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        if (TextValidation.TryFindUnpairedSurrogate(text, out int surrogateIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                BinaryErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {surrogateIndex}.");
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);

        int prefixLength = includeBytePrefix ? 2 : 0;
        int separatorLength = format == BinaryFormat.Spaced ? 1 : 0;
        long outputLength = (long)bytes.Length * (8 + prefixLength) + ((long)Math.Max(0, bytes.Length - 1) * separatorLength);

        if (outputLength > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                BinaryErrorCodes.InputTooLarge,
                $"The binary representation would be {outputLength} characters; the limit is {MaxTextLengthLimit}. Use the compact format or a shorter input.");
        }

        var builder = new StringBuilder((int)outputLength);

        for (int index = 0; index < bytes.Length; index++)
        {
            if (index > 0 && separatorLength == 1)
                builder.Append(' ');

            if (includeBytePrefix)
                builder.Append("0b");

            byte value = bytes[index];

            for (int bit = 7; bit >= 0; bit--)
                builder.Append((value >> bit) & 1);
        }

        return ToolResult<string>.Ok(builder.ToString());
    }

    public ToolResult<string> Decode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                BinaryErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        var bytes = new List<byte>(text.Length / 8 + 1);
        int bitCount = 0;
        byte current = 0;

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (char.IsWhiteSpace(character) || character == '_')
                continue;

            // "0b" öneki: '0' veri biti olarak sayılmaz, yoksa önek veriye karışırdı.
            // 'b' hiçbir zaman geçerli bir bit olmadığı için bu okuma kesindir.
            if (character == '0' && index + 1 < text.Length && (text[index + 1] == 'b' || text[index + 1] == 'B'))
            {
                index++;
                continue;
            }

            if (character != '0' && character != '1')
            {
                return ToolResult<string>.Fail(
                    ToolErrorKind.InvalidFormat,
                    BinaryErrorCodes.InvalidDigit,
                    $"Unexpected character '{character}' (U+{(int)character:X4}) at index {index}; binary input accepts only 0 and 1.");
            }

            current = (byte)((current << 1) | (character - '0'));
            bitCount++;

            if (bitCount % 8 == 0)
            {
                bytes.Add(current);
                current = 0;
            }
        }

        if (bitCount % 8 != 0)
        {
            // Eskiden bu artık bitler sessizce atılıyordu.
            int leftover = bitCount % 8;

            return ToolResult<string>.Fail(
                ToolErrorKind.InvalidFormat,
                BinaryErrorCodes.OddBitCount,
                $"The payload contains {bitCount} bits, which is not a multiple of 8; the last byte is missing {8 - leftover} bit(s).");
        }

        if (bytes.Count > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                BinaryErrorCodes.OutputTooLarge,
                $"Decoded size {bytes.Count} bytes exceeds {MaxTextLengthLimit}.");
        }

        try
        {
            return ToolResult<string>.Ok(TextEncodings.StrictUtf8.GetString(bytes.ToArray()));
        }
        catch (DecoderFallbackException ex)
        {
            return ToolResult<string>.Fail(ToolErrorKind.InvalidFormat, BinaryErrorCodes.NotUtf8, ex.Message);
        }
    }
}
