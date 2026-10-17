using System.Text;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Harf döndürme (ROT-N) işleminin tek uygulaması.
/// <para>
/// Yalnızca ASCII harfleri döndürülür; Türkçe harfler (ğ, ü, ş, ı, ö, ç), rakamlar, noktalama ve
/// emoji gibi karakterler olduğu gibi kalır. Bu, ROT13'ün tanımıdır; araç bunu kullanıcıya bildirir.
/// İşlem gizlilik sağlamaz, yalnızca içeriği okunmaz hâle getirir.
/// </para>
/// </summary>
public sealed class Rot13Service : IRot13Service
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;

    private const int AlphabetLength = 26;

    public int MaxTextLength => MaxTextLengthLimit;

    public ToolResult<string> Rotate(string? text, int shift)
    {
        if (string.IsNullOrEmpty(text))
            return ToolResult<string>.Ok(string.Empty);

        if (text.Length > MaxTextLengthLimit)
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.TooLarge,
                Rot13ErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        if (TextValidation.TryFindUnpairedSurrogate(text, out int surrogateIndex))
        {
            return ToolResult<string>.Fail(
                ToolErrorKind.Validation,
                Rot13ErrorCodes.InvalidUnicode,
                $"Unpaired surrogate at index {surrogateIndex}.");
        }

        // Negatif ve 26'dan büyük kaydırmalar da geçerlidir; 0 ise metin aynen döner.
        int normalized = ((shift % AlphabetLength) + AlphabetLength) % AlphabetLength;

        if (normalized == 0)
            return ToolResult<string>.Ok(text);

        var builder = new StringBuilder(text.Length);

        foreach (char character in text)
        {
            if (character >= 'a' && character <= 'z')
            {
                builder.Append((char)('a' + ((character - 'a' + normalized) % AlphabetLength)));
            }
            else if (character >= 'A' && character <= 'Z')
            {
                builder.Append((char)('A' + ((character - 'A' + normalized) % AlphabetLength)));
            }
            else
            {
                // Harf olmayan ve ASCII dışı karakterler değişmeden geçer; surrogate çiftleri bozulmaz.
                builder.Append(character);
            }
        }

        return ToolResult<string>.Ok(builder.ToString());
    }
}
