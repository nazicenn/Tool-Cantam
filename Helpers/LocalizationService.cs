using System.Globalization;
using System.Windows;

namespace tool_cantam.Helpers;

/// <summary>
/// WPF kaynak sözlükleri üzerinden çalışan dil altyapısı.
/// Uygulama kodu bu sınıfı doğrudan kullanmamalı; <c>ILocalizerService</c> enjekte edilmelidir.
/// Burası yalnızca kaynak sözlüğünü değiştiren ve anahtar çözen alt katmandır.
/// </summary>
public static class LocalizationService
{
    public const string DefaultCultureCode = "en-US";

    /// <summary>Desteklenen dil kodlarının resmî biçimi.</summary>
    public static readonly IReadOnlyList<string> SupportedCultureCodes = new[] { "en-US", "tr-TR" };

    /// <summary>
    /// Kullanıcı/ayar kaynaklı dil adlarını resmî koda eşler.
    /// Ayar dosyasındaki eski "English"/"Türkçe" değerleri de burada karşılanır.
    /// </summary>
    private static readonly Dictionary<string, string> CultureAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en-US"] = "en-US",
        ["en"] = "en-US",
        ["English"] = "en-US",
        ["İngilizce"] = "en-US",
        ["tr-TR"] = "tr-TR",
        ["tr"] = "tr-TR",
        ["Türkçe"] = "tr-TR",
        ["Turkce"] = "tr-TR",
        ["Turkish"] = "tr-TR"
    };

    private static string _currentCulture = DefaultCultureCode;

    /// <summary>Aktif dil kodu.</summary>
    public static string CurrentCulture => _currentCulture;

    /// <summary>Son dil değiştirme denemesinin hatası; başarılıysa null.</summary>
    public static string? LastError { get; private set; }

    /// <summary>Verilen değeri desteklenen bir dil koduna çevirmeyi dener.</summary>
    public static bool TryNormalizeCulture(string? value, out string cultureCode)
    {
        cultureCode = DefaultCultureCode;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!CultureAliases.TryGetValue(value.Trim(), out string? normalized))
            return false;

        cultureCode = normalized;
        return true;
    }

    /// <summary>
    /// Anahtarı aktif dildeki metne çevirir; bulunamazsa anahtarın kendisini döndürür.
    /// </summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        return Application.Current?.TryFindResource(key) as string ?? key;
    }

    /// <summary>
    /// Aktif dili değiştirir: kültürü ayarlar ve dil kaynak sözlüğünü değiştirir.
    /// Hata durumunda mevcut dil korunur ve <see cref="LastError"/> doldurulur.
    /// </summary>
    public static bool SetLanguage(string? cultureCode)
    {
        if (!TryNormalizeCulture(cultureCode, out string normalized))
        {
            LastError = $"Unsupported culture code: '{cultureCode}'.";
            return false;
        }

        var application = Application.Current;
        if (application?.Resources is null)
        {
            LastError = "Application resources are not available yet.";
            return false;
        }

        try
        {
            var culture = new CultureInfo(normalized);
            var dictionary = new ResourceDictionary
            {
                // Mutlak pack URI, göreli yollara göre ortamdan bağımsızdır.
                Source = new Uri($"pack://application:,,,/Resources/Languages/{normalized}.xaml", UriKind.Absolute)
            };

            var merged = application.Resources.MergedDictionaries;
            var existing = merged.FirstOrDefault(d =>
                d.Source is not null &&
                d.Source.OriginalString.Contains("/Resources/Languages/", StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                merged.Remove(existing);

            merged.Add(dictionary);

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            _currentCulture = normalized;
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }
}
