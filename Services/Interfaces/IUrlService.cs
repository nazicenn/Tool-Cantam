using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// URL yüzde kodlama/çözme işlemlerinin sözleşmesi. Tüm metotlar istisna fırlatmak yerine
/// <see cref="ToolResult{T}"/> döndürür ve kodlama bağlamı açıkça belirtilir.
/// </summary>
public interface IUrlService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Metni seçilen bağlama göre yüzde kodlar.</summary>
    ToolResult<string> Encode(string? text, UrlEncodingMode mode = UrlEncodingMode.Component);

    /// <summary>
    /// Yüzde kodlanmış metni çözer. Geçersiz kaçış dizileri, bozuk UTF-8 ve eşleşmemiş
    /// surrogate çiftleri sessizce değiştirilmez; sınıflandırılmış hata döner.
    /// </summary>
    ToolResult<string> Decode(string? text, UrlEncodingMode mode = UrlEncodingMode.Component);
}

/// <summary>
/// URL aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.
/// </summary>
public static class UrlErrorCodes
{
    public const string InvalidEscape = "L.Url.Error.InvalidEscape";
    public const string InvalidUnicode = "L.Url.Error.InvalidUnicode";
    public const string NotUtf8 = "L.Url.Error.NotUtf8";
    public const string InputTooLarge = "L.Url.Error.InputTooLarge";
    public const string OutputTooLarge = "L.Url.Error.OutputTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
