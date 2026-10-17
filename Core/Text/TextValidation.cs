namespace tool_cantam.Core.Text;

/// <summary>
/// Metin doğrulama yardımcıları. Kodlayıcı araçlar aynı kuralları paylaşır.
/// </summary>
public static class TextValidation
{
    /// <summary>
    /// Eşleşmemiş surrogate (bozuk Unicode) arar. Kodlama yapan araçlar bu durumu sessizce
    /// U+FFFD ile değiştirmek yerine hata olarak bildirir.
    /// </summary>
    /// <param name="text">Denetlenecek metin.</param>
    /// <param name="index">İlk bozuk karakterin konumu; hata yoksa -1.</param>
    public static bool TryFindUnpairedSurrogate(string text, out int index)
    {
        for (int position = 0; position < text.Length; position++)
        {
            char character = text[position];

            if (char.IsHighSurrogate(character))
            {
                if (position + 1 >= text.Length || !char.IsLowSurrogate(text[position + 1]))
                {
                    index = position;
                    return true;
                }

                position++;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                index = position;
                return true;
            }
        }

        index = -1;
        return false;
    }
}
