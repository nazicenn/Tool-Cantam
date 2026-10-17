namespace tool_cantam.Core.Text;

/// <summary>
/// Bayt dizilerinin onaltılık gösterimi. Araçların büyük/küçük harf konusunda tutarlı olması için
/// (hash, HMAC, hex kodlayıcı) tek yerde tutulur.
/// </summary>
public static class HexFormatting
{
    /// <summary>Büyük harfli onaltılık gösterim (varsayılan).</summary>
    public static string ToHexUpper(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes);

    /// <summary>Küçük harfli onaltılık gösterim.</summary>
    public static string ToHexLower(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(bytes);

    /// <summary>
    /// Karşılaştırma için onaltılık metni normalize eder: boşluk, iki nokta, tire ve alt çizgi atılır,
    /// harfler büyütülür. Kullanıcıların farklı biçimlerde yapıştırdığı özetler böylece eşleşir.
    /// </summary>
    public static string NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length);

        foreach (char character in value)
        {
            if (character is ' ' or '\t' or '\r' or '\n' or ':' or '-' or '_')
                continue;

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
