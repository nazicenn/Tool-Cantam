namespace tool_cantam.Core.Enums;

/// <summary>Beklenen özet ile hesaplanan özetin karşılaştırma durumu.</summary>
public enum HashMatchState
{
    /// <summary>Karşılaştırma için bir özet girilmedi.</summary>
    NotChecked,

    /// <summary>Özet eşleşiyor.</summary>
    Matched,

    /// <summary>Özet eşleşmiyor.</summary>
    Mismatched,

    /// <summary>
    /// Girilen özet bu algoritmayla karşılaştırılamaz (uzunluk farklı veya geçerli hex değil).
    /// "Tüm algoritmalar" modunda bu, özetin hangi algoritmaya ait olduğunu daraltmaya yarar.
    /// </summary>
    NotComparable
}
