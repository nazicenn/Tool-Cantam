using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Harf döndürme (ROT-N) işlemlerinin sözleşmesi.
/// <para>
/// <b>Bu bir şifreleme yöntemi değildir.</b> Yalnızca okunabilirliği bozan bir dönüşümdür;
/// gizlilik veya bütünlük sağlamaz. Araç bu gerçeği kullanıcıya açıkça göstermelidir.
/// </para>
/// </summary>
public interface IRot13Service
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>
    /// Yalnızca ASCII harfleri (A-Z, a-z) döndürür; diğer tüm karakterler değişmeden geçer.
    /// </summary>
    /// <param name="shift">
    /// Kaydırma miktarı. 26'ya göre normalleştirilir; 13 için işlem kendi tersidir
    /// (kodlama ve çözme aynı sonucu verir).
    /// </param>
    ToolResult<string> Rotate(string? text, int shift);
}

/// <summary>ROT aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
