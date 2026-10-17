namespace tool_cantam.Core.Enums;

/// <summary>Hex çıktısının yazım biçimi. Çözme tarafı tüm biçimleri kabul eder.</summary>
public enum HexFormat
{
    /// <summary>Ayırıcısız: <c>48656C6C6F</c></summary>
    Compact,

    /// <summary>Baytlar arasında boşluk: <c>48 65 6C 6C 6F</c></summary>
    Spaced,

    /// <summary>Baytlar arasında iki nokta: <c>48:65:6C:6C:6F</c></summary>
    ColonSeparated
}
