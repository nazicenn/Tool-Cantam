using System.Collections.Generic;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Metin ters çevirme sözleşmesi. Tüm dönüşümler <see cref="ToolResult{T}"/> ile döner;
/// boyut sınırı ve geçersiz Unicode sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IStringReverserService
{
    int MaxTextLength { get; }
    IReadOnlyList<StringReverseMode> SupportedModes { get; }

    ToolResult<string> Reverse(string? text, StringReverseMode mode);
}

/// <summary>Ters çevirme modları.</summary>
public enum StringReverseMode
{
    /// <summary>Tüm karakterleri ters çevir: "abc" → "cba"</summary>
    ReverseAll,

    /// <summary>Kelime sırasını ters çevir: "a b c" → "c b a"</summary>
    ReverseWords,

    /// <summary>Her kelimeyi ters çevir: "abc def" → "cba fed"</summary>
    ReverseEachWord,

    /// <summary>Satır sırasını ters çevir: "l1\nl2" → "l2\nl1"</summary>
    ReverseLines,

    /// <summary>Satır sırasını ve içeriği ters çevir</summary>
    ReverseLinesAndContent
}