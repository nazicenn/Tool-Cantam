using CommunityToolkit.Mvvm.ComponentModel;
using tool_cantam.Core.Enums;

namespace tool_cantam.ViewModels;

/// <summary>
/// Tek bir algoritma için hesaplanan özet satırı. Ham özet baytları satırda tutulmaz; gösterim ve
/// karşılaştırma için gereken metin biçimleri saklanır, baytlar oluşturulduktan sonra sıfırlanır.
/// </summary>
public sealed partial class HashResultRow : ObservableObject
{
    public HashResultRow(string algorithmName, string digest, string canonicalHex, bool isWeak, int hexLength)
    {
        AlgorithmName = algorithmName;
        Digest = digest;
        CanonicalHex = canonicalHex;
        IsWeak = isWeak;
        HexLength = hexLength;
    }

    /// <summary>Algoritmanın görünen adı (ör. SHA-256).</summary>
    public string AlgorithmName { get; }

    /// <summary>Seçilen biçimde özet (hex veya Base64).</summary>
    public string Digest { get; }

    /// <summary>Karşılaştırma için kanonik biçim: büyük harfli hex.</summary>
    public string CanonicalHex { get; }

    /// <summary>Algoritma güvenlik amaçlı kullanılmamalı mı (MD5, SHA-1).</summary>
    public bool IsWeak { get; }

    /// <summary>Özetin hex gösterim uzunluğu; karşılaştırılabilirliği belirler.</summary>
    public int HexLength { get; }

    [ObservableProperty] private HashMatchState _match = HashMatchState.NotChecked;

    [ObservableProperty] private string _matchText = string.Empty;
}
