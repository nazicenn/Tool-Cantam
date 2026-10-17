using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Bir özet değerinin hangi algoritmaya ait olabileceğini biçiminden çıkarır.
/// Tüm metotlar istisna fırlatmak yerine <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IHashIdentifierService
{
    /// <summary>İncelenecek en büyük karakter sayısı.</summary>
    int MaxInputLength { get; }

    /// <summary>
    /// Girdinin uzunluğuna, karakter kümesine ve ayırt edici önekine göre aday algoritmaları döndürür.
    /// Hiçbir aday bulunamazsa sonuç başarılıdır ama <see cref="HashIdentificationResult.IsIdentified"/>
    /// false olur.
    /// </summary>
    ToolResult<HashIdentificationResult> Identify(string? value);
}

/// <summary>Tanımlayıcı aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class HashIdentifierErrorCodes
{
    public const string EmptyInput = "L.HashIdentifier.Error.EmptyInput";
    public const string InputTooLong = "L.HashIdentifier.Error.InputTooLong";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
