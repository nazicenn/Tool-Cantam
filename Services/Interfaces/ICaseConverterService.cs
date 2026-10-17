using System.Collections.Generic;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Metin büyük/küçük harf dönüşümü sözleşmesi.
/// Tüm dönüşümler <see cref="ToolResult{T}"/> ile döner; geçersiz Unicode
/// ve boyut sınırı sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface ICaseConverterService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Desteklenen dönüşüm modları.</summary>
    IReadOnlyList<CaseConversionMode> SupportedModes { get; }

    /// <summary>Metni belirtilen moda çevirir.</summary>
    ToolResult<string> Convert(string? text, CaseConversionMode mode);
}

/// <summary>Harf dönüşüm modları.</summary>
public enum CaseConversionMode
{
    Upper,
    Lower,
    Title,
    Sentence,
    Camel,
    Pascal,
    Snake,
    Kebab,
    Alternating,
    Invert
}