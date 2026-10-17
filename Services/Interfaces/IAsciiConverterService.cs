using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Metin ile karakter kod noktaları arasında dönüşüm sözleşmesi.
/// <para>
/// Araç "ASCII" ile sınırlı değildir: ASCII bu kümenin ilk 128 karakteridir. Dönüşüm,
/// UTF-16 kod birimi yerine gerçek Unicode <b>kod noktaları</b> üzerinde çalışır; bu yüzden
/// emoji tek bir sayı olarak temsil edilir (iki vekil kod birimi olarak değil).
/// </para>
/// </summary>
public interface IAsciiConverterService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Metni kod noktalarına çevirir (boşlukla ayrılmış).</summary>
    ToolResult<string> ToCodes(string? text, CharacterCodeFormat format = CharacterCodeFormat.Decimal);

    /// <summary>
    /// Kod noktalarını metne çevirir. Ayırıcı olarak boşluk, sekme, satır sonu, virgül, noktalı virgül
    /// ve dikey çizgi kabul edilir. <c>U+</c>, <c>0x</c>, <c>\u</c> ve <c>\x</c> önekleri onaltılık
    /// olduğunu belirtir; öneksiz değerler seçilen biçime göre yorumlanır.
    /// </summary>
    ToolResult<string> ToText(string? codes, CharacterCodeFormat defaultFormat = CharacterCodeFormat.Decimal);
}

/// <summary>Karakter kodu aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class CharacterCodeErrorCodes
{
    public const string InvalidToken = "L.CharacterCodes.Error.InvalidToken";
    public const string InvalidCodePoint = "L.CharacterCodes.Error.InvalidCodePoint";
    public const string InvalidUnicode = "L.CharacterCodes.Error.InvalidUnicode";
    public const string InputTooLarge = "L.CharacterCodes.Error.InputTooLarge";
    public const string OutputTooLarge = "L.CharacterCodes.Error.OutputTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
