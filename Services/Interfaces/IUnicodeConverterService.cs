using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;

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
