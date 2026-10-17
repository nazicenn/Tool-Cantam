using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Kriptografik özet (hash) hesaplama sözleşmesi. Servis yalnızca hesaplar ve özetin ham baytlarını
/// döndürür; gösterim biçimi (hex/Base64) sunum katmanının kararıdır. Böylece aynı özet hem
/// görüntülenebilir hem de karşılaştırma için kanonik biçimde tutulabilir.
/// </summary>
public interface IHashService
{
    /// <summary>Metin girdisi için en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>
    /// Bu makinede gerçekten kullanılabilen algoritmalar. İşletim sistemi bir algoritmayı
    /// desteklemiyorsa listede görünmez.
    /// </summary>
    IReadOnlyList<HashAlgorithmDescriptor> Algorithms { get; }

    /// <summary>Metnin UTF-8 baytlarının özetini hesaplar.</summary>
    ToolResult<byte[]> ComputeFromText(string? text, HashAlgorithmKind kind);

    /// <summary>
    /// Dosyanın özetini akış halinde hesaplar; bellek kullanımı dosya boyutundan bağımsızdır.
    /// </summary>
    Task<ToolResult<byte[]>> ComputeFromFileAsync(
        string filePath,
        HashAlgorithmKind kind,
        CancellationToken cancellationToken = default);
}

/// <summary>Hash aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class HashErrorCodes
{
    public const string UnsupportedAlgorithm = "L.Hash.Error.UnsupportedAlgorithm";
    public const string AlgorithmUnavailable = "L.Hash.Error.AlgorithmUnavailable";
    public const string InputTooLarge = "L.Hash.Error.InputTooLarge";
    public const string PathRequired = "L.Hash.Error.PathRequired";
    public const string FileNotFound = "L.Hash.Error.FileNotFound";
    public const string IoError = "L.Hash.Error.IoError";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
