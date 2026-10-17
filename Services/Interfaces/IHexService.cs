using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Hex kodlama/çözme işlemlerinin sözleşmesi. Tüm metotlar istisna fırlatmak yerine
/// <see cref="ToolResult{T}"/> döndürür.
/// </summary>
public interface IHexService
{
    /// <summary>İşlenecek en büyük karakter sayısı (hem metin hem hex girdisi için).</summary>
    int MaxTextLength { get; }

    /// <summary>Metni UTF-8 baytlarına çevirip seçilen biçimde hex olarak yazar.</summary>
    ToolResult<string> Encode(string? text, HexFormat format = HexFormat.Compact, bool uppercase = true);

    /// <summary>
    /// Hex metnini çözer. Boşluk, iki nokta, virgül, tire, alt çizgi ayırıcı olarak kabul edilir;
    /// <c>0x</c> ve <c>\x</c> önekleri yok sayılır. Tek sayıda basamak, geçersiz basamak veya
    /// geçersiz UTF-8 sessizce değiştirilmez, hata döner.
    /// </summary>
    ToolResult<string> Decode(string? text);
}

/// <summary>Hex aracının hata kodları; her kod aynı zamanda yerelleştirme anahtarıdır.</summary>
public static class HexErrorCodes
{
    public const string InvalidDigit = "L.Hex.Error.InvalidDigit";
    public const string OddLength = "L.Hex.Error.OddLength";
    public const string NotUtf8 = "L.Hex.Error.NotUtf8";
    public const string InvalidUnicode = "L.Hex.Error.InvalidUnicode";
    public const string InputTooLarge = "L.Hex.Error.InputTooLarge";
    public const string OutputTooLarge = "L.Hex.Error.OutputTooLarge";
    public const string Unexpected = "L.Common.Error.Unexpected";
}
