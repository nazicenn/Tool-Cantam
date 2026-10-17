using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Metin ile Unicode kaçış dizileri arasındaki dönüşümün sözleşmesi.
/// Tüm metotlar istisna fırlatmak yerine <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IUnicodeConverterService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Metni seçilen biçimde kaçış dizilerine çevirir.</summary>
    ToolResult<string> Escape(string? text, UnicodeEscapeFormat format = UnicodeEscapeFormat.Utf16Escape);

    /// <summary>
    /// Kaçış dizilerini metne çevirir. <c>\uXXXX</c>, <c>\u{...}</c>, <c>\UXXXXXXXX</c>,
    /// <c>\xXX</c>, <c>U+XXXX</c> ve yaygın C kaçışları (<c>\n</c>, <c>\t</c>, <c>\\</c> ...) tanınır.
    /// Eşleşmemiş vekil, geçersiz kod noktası ve bozuk UTF-8 bayt dizisi hata olarak bildirilir.
    /// </summary>
    ToolResult<string> Unescape(string? text);
}

/// <summary>Unicode aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class UnicodeErrorCodes
{
    public const string InvalidEscape = "L.Unicode.Error.InvalidEscape";
    public const string InvalidCodePoint = "L.Unicode.Error.InvalidCodePoint";
    public const string LoneSurrogate = "L.Unicode.Error.LoneSurrogate";
    public const string NotUtf8 = "L.Unicode.Error.NotUtf8";
    public const string InvalidUnicode = "L.Unicode.Error.InvalidUnicode";
    public const string InputTooLarge = "L.Unicode.Error.InputTooLarge";
    public const string OutputTooLarge = "L.Unicode.Error.OutputTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
