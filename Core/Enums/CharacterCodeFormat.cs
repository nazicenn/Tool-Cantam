namespace tool_cantam.Core.Enums;

/// <summary>Karakter kodu gösterim biçimi. Çözme tarafı iki biçimi de kabul eder.</summary>
public enum CharacterCodeFormat
{
    /// <summary>Ondalık kod noktası: <c>65 8364 128512</c></summary>
    Decimal,

    /// <summary>Onaltılık kod noktası, <c>U+</c> önekli: <c>U+0041 U+20AC U+1F600</c></summary>
    Hexadecimal
}
