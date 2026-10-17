namespace tool_cantam.Core.Text;

/// <summary>
/// Onaltılık basamak işlemleri. Metin kodlayıcı araçların (URL, Hex, Base64, hash gösterimi)
/// aynı kuralları kullanması için tek yerde tutulur; her araç kendi kopyasını taşımaz.
/// </summary>
public static class HexDigits
{
    public const string Uppercase = "0123456789ABCDEF";

    public const string Lowercase = "0123456789abcdef";

    /// <summary>Bir karakteri onaltılık değere çevirir; hem küçük hem büyük harf kabul edilir.</summary>
    public static bool TryParse(char character, out int value)
    {
        switch (character)
        {
            case >= '0' and <= '9':
                value = character - '0';
                return true;
            case >= 'a' and <= 'f':
                value = character - 'a' + 10;
                return true;
            case >= 'A' and <= 'F':
                value = character - 'A' + 10;
                return true;
            default:
                value = 0;
                return false;
        }
    }

    /// <summary>
    /// Hex metinlerinde yaygın olarak kullanılan ayırıcılar: boşluk, iki nokta, virgül, tire, alt çizgi.
    /// Bunlar veri taşımaz, yalnızca okunabilirliği artırır.
    /// </summary>
    public static bool IsSeparator(char character) =>
        char.IsWhiteSpace(character) || character is ':' or ',' or '-' or '_';
}
