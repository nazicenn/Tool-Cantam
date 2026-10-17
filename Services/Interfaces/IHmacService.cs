using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// HMAC hesaplama ve doğrulama sözleşmesi. Tüm metotlar istisna fırlatmak yerine
/// <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IHmacService
{
    /// <summary>Mesaj için en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Anahtar için en büyük bayt sayısı.</summary>
    int MaxKeyBytes { get; }

    /// <summary>Bu makinede kullanılabilen HMAC algoritmaları (zayıf olanlar işaretli).</summary>
    IReadOnlyList<HashAlgorithmDescriptor> Algorithms { get; }

    /// <summary>
    /// Mesajın HMAC değerini hesaplar. Anahtar ve ara tamponlar iş bitiminde sıfırlanır.
    /// </summary>
    ToolResult<byte[]> Compute(string? message, string? key, HmacKeyFormat keyFormat, HashAlgorithmKind kind);

    /// <summary>
    /// Beklenen MAC değerini çözer: onaltılık (boşluk/iki nokta yok sayılır) veya Base64.
    /// Karşılaştırma çağıran tarafta <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals"/>
    /// ile yapılmalıdır; düz metin karşılaştırması zamanlama sızıntısı üretir.
    /// </summary>
    bool TryParseMac(string? value, out byte[] mac);
}

/// <summary>HMAC aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class HmacErrorCodes
{
    public const string EmptyKey = "L.Hmac.Error.EmptyKey";
    public const string InvalidHexKey = "L.Hmac.Error.InvalidHexKey";
    public const string KeyTooLong = "L.Hmac.Error.KeyTooLong";
    public const string InputTooLarge = "L.Hmac.Error.InputTooLarge";
    public const string UnsupportedAlgorithm = "L.Hmac.Error.UnsupportedAlgorithm";
    public const string AlgorithmUnavailable = "L.Hmac.Error.AlgorithmUnavailable";
    public const string InvalidExpectedMac = "L.Hmac.Error.InvalidExpectedMac";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
