namespace tool_cantam.Core.Enums;

/// <summary>HMAC anahtarının nasıl yorumlanacağı.</summary>
public enum HmacKeyFormat
{
    /// <summary>Anahtar metin olarak verilir; UTF-8 baytları kullanılır.</summary>
    Text,

    /// <summary>Anahtar onaltılık gösterimle verilir (aradaki boşluk ve iki nokta yok sayılır).</summary>
    Hex
}
