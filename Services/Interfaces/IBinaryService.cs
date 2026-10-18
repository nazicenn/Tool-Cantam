using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// İkili (binary) kodlama/çözme işlemlerinin sözleşmesi. Tüm metotlar istisna fırlatmak yerine
/// <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IBinaryService
{
    /// <summary>
    /// İkili gösterimin en büyük karakter sayısı. Metin kodlarken bu sınır, üretilecek gösterim
    /// için de geçerlidir; bu yüzden kodlanabilen metin boyutu biçime göre değişir.
    /// </summary>
    int MaxTextLength { get; }

    /// <summary>Metni UTF-8 baytlarına çevirip her baytı 8 bit olarak yazar.</summary>
    /// <param name="format">Baytlar arasında boşluk olsun mu.</param>
    /// <param name="includeBytePrefix">Her bayt <c>0b</c> önekiyle yazılsın mı.</param>
    ToolResult<string> Encode(string? text, BinaryFormat format = BinaryFormat.Compact, bool includeBytePrefix = false);

    /// <summary>
    /// İkili metni çözer. Boşluk, alt çizgi ve <c>0b</c> önekleri yok sayılır.
    /// Bit sayısı 8'in katı değilse veya geçersiz karakter varsa sessizce veri atlanmaz; hata döner.
    /// </summary>
    ToolResult<string> Decode(string? text);
}

/// <summary>İkili araç hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
