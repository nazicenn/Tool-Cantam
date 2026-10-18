using tool_cantam.Services.ErrorCodes;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Base64 kodlama/çözme işlemlerinin tek sözleşmesi. Metin ve dosya işlemleri aynı servistedir;
/// kod tabanındaki dağınık Base64 kopyaları zamanla buraya taşınır.
/// Tüm metotlar istisna fırlatmak yerine <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IBase64Service
{
    /// <summary>Metin işlemlerinde izin verilen en büyük UTF-8 bayt sayısı.</summary>
    int MaxTextBytes { get; }

    /// <summary>Metni Base64'e kodlar. Boş girdi boş çıktı üretir (hata değildir).</summary>
    ToolResult<string> EncodeText(string plainText, Base64Variant variant = Base64Variant.Standard);

    /// <summary>
    /// Base64 metnini çözer. Boşluk/satır sonu karakterleri yok sayılır, eksik dolgu tamamlanır.
    /// Çözülen baytlar geçerli UTF-8 değilse sessizce bozuk metin üretmek yerine hata döner.
    /// </summary>
    ToolResult<string> DecodeText(string base64Text, Base64Variant variant = Base64Variant.Standard);

    /// <summary>
    /// Dosyayı akış halinde Base64'e kodlar; bellek kullanımı dosya boyutundan bağımsızdır.
    /// Çıktı geçici dosyaya yazılır ve yalnızca başarılı bitişte hedefe taşınır.
    /// </summary>
    Task<ToolResult<long>> EncodeFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Base64 dosyasını akış halinde çözer. Satır sonları yok sayılır (PEM/e-posta gövdeleriyle uyumlu).
    /// </summary>
    Task<ToolResult<long>> DecodeFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base64 aracının hata kodları. Her kod aynı zamanda yerelleştirme anahtarıdır;
/// teknik ayrıntı kullanıcıya değil loga gider.
/// </summary>

