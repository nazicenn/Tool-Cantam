using tool_cantam.Core.Enums;

namespace tool_cantam.Core.Models;

/// <summary>Biçimden tanımlanabilen tek bir algoritma adayı.</summary>
public sealed record HashCandidate(string Algorithm, HashIdentifierConfidence Confidence);

/// <summary>
/// Bir hash tanımlama sonucu: girdinin ölçülebilir özellikleri ve aday algoritmalar.
/// <para>
/// Araç hiçbir zaman "bu hash şudur" demez; biçimden yalnızca <b>adaylar</b> çıkarır. Aynı uzunluk ve
/// karakter kümesine sahip algoritmalar (ör. 32 hex: MD5, NTLM, MD4) ayırt edilemez.
/// </para>
/// </summary>
public sealed record HashIdentificationResult(
    int Length,
    HashCharacterSet CharacterSet,
    bool HasDistinctivePrefix,
    IReadOnlyList<HashCandidate> Candidates)
{
    public bool IsIdentified => Candidates.Count > 0;

    /// <summary>Birden çok aday var ve aralarında seçim yapılamıyor.</summary>
    public bool IsAmbiguous => Candidates.Count > 1;
}
