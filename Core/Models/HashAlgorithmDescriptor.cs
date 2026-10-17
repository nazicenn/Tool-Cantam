using tool_cantam.Core.Enums;

namespace tool_cantam.Core.Models;

/// <summary>
/// Bir hash algoritmasının meta verisi. <see cref="IsWeak"/> true ise algoritma güvenlik amaçlı
/// kullanılmamalıdır (MD5 ve SHA-1 çakışma saldırılarına karşı kırılmıştır); arayüz bunu kullanıcıya
/// açıkça göstermek zorundadır.
/// </summary>
public sealed record HashAlgorithmDescriptor(
    HashAlgorithmKind Kind,
    string DisplayName,
    string AlgorithmName,
    int DigestSizeBytes,
    bool IsWeak)
{
    /// <summary>Özet uzunluğunun onaltılık gösterimdeki karakter sayısı.</summary>
    public int HexLength => DigestSizeBytes * 2;
}
