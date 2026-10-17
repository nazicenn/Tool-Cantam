namespace tool_cantam.Core.Models;

public sealed class AppSettings
{
    public bool IsDarkTheme { get; set; } = true;

    /// <summary>Aktif dil kodu (ör. en-US, tr-TR). Eski "English"/"Türkçe" değerleri de okunabilir.</summary>
    public string Language { get; set; } = "en-US";
}
