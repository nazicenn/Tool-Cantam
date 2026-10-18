using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// HTML kodlama/çözme işlemlerinin sözleşmesi. Tüm metotlar istisna fırlatmak yerine
/// <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IHtmlEncoderService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>
    /// Metni HTML için kodlar. Her zaman beş kritik karakter (&amp;, &lt;, &gt;, ", ') kodlanır;
    /// böylece çıktı hem metin gövdesinde hem de tırnaklı öznitelik değerlerinde güvenlidir.
    /// </summary>
    /// <param name="useNamedEntities">true ise <c>&amp;amp;</c> gibi adlar, false ise <c>&amp;#38;</c> gibi sayısal referanslar yazılır.</param>
    /// <param name="encodeNonAscii">true ise ASCII dışı karakterler de sayısal referansa çevrilir (çıktı saf ASCII olur).</param>
    ToolResult<string> Encode(string? text, bool useNamedEntities = true, bool encodeNonAscii = false);

    /// <summary>
    /// HTML varlıklarını çözer: adlandırılmış varlıklar, ondalık ve onaltılık sayısal referanslar.
    /// </summary>
    /// <param name="strictEntities">
    /// true ise tanınmayan veya noktalı virgülü eksik varlıklar hata olarak bildirilir;
    /// false ise olduğu gibi bırakılır.
    /// </param>
    ToolResult<string> Decode(string? text, bool strictEntities = true);
}

/// <summary>HTML aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
