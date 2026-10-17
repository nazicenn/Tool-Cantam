namespace tool_cantam.Core.Enums;

/// <summary>
/// Bir hash tanımlamasının güven düzeyi. Yalnızca biçimden (uzunluk/karakter kümesi) tanımlama
/// hiçbir zaman kesin değildir; bu yüzden <see cref="Certain"/> yalnızca ayırt edici önek
/// (<c>$2b$</c>, <c>$argon2id$</c>, <c>{SSHA}</c> gibi) bulunduğunda kullanılır.
/// </summary>
public enum HashIdentifierConfidence
{
    /// <summary>Ayırt edici önek sayesinde kesin.</summary>
    Certain,

    /// <summary>Biçim tek bir algoritmaya işaret ediyor.</summary>
    Likely,

    /// <summary>Biçim birden çok algoritmaya uyuyor; biri seçilemez.</summary>
    Possible
}
