using System.Text;

namespace tool_cantam.Core.Text;

/// <summary>
/// Metin kodlayıcı araçların ortak kodlama nesneleri.
/// </summary>
public static class TextEncodings
{
    /// <summary>
    /// Geçersiz bayt dizilerini sessizce U+FFFD'ye çevirmek yerine hata veren UTF-8 kod çözücü.
    /// İkili veri metin aracıyla çözülmeye çalışıldığında bozuk metin üretilmemesi için kullanılır.
    /// </summary>
    public static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
