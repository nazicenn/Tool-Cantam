namespace tool_cantam.Core.Enums;

/// <summary>İkili (binary) gösterimin yazım biçimi. Çözme tarafı tüm biçimleri kabul eder.</summary>
public enum BinaryFormat
{
    /// <summary>Baytlar arasında boşluk: <c>01001000 01101001</c></summary>
    Spaced,

    /// <summary>Ayırıcısız: <c>0100100001101001</c></summary>
    Compact
}
