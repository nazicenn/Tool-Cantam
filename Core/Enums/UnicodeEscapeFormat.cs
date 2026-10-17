namespace tool_cantam.Core.Enums;

/// <summary>
/// Unicode kaçış gösterimi biçimleri. Çözme tarafı hepsini (ve <c>\u{...}</c> biçimini) kabul eder.
/// </summary>
public enum UnicodeEscapeFormat
{
    /// <summary>
    /// <c>\uXXXX</c> (UTF-16 kod birimi). BMP dışındaki karakterler vekil çifti olarak yazılır:
    /// <c>\uD83D\uDE00</c>. JavaScript, Java ve C# dizeleriyle uyumludur.
    /// </summary>
    Utf16Escape,

    /// <summary>
    /// <c>\U0001F600</c> (sekiz onaltılık basamak, kod noktası başına bir kaçış).
    /// Python ve C++ dizeleriyle uyumludur.
    /// </summary>
    CodePointEscape,

    /// <summary>
    /// <c>\xE2\x82\xAC</c> (UTF-8 baytları, iki onaltılık basamak). C/C++ dizeleriyle uyumludur.
    /// </summary>
    Utf8ByteEscape,

    /// <summary>
    /// <c>U+20AC</c> (okunabilir kod noktası gösterimi). Karakter kodu dönüştürücüsünün çıktısıyla aynıdır.
    /// </summary>
    CodePointNotation
}
