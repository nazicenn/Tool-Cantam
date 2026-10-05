using System.Globalization;
using System.Windows;

namespace tool_cantam.Helpers;

public static class LocalizationService
{
    private const string LangPathPart = "/Resources/Languages/";
    private static string? _currentCulture;

    public static void SetLanguage(string cultureCode)
    {
        // 1. Aynı dil zaten seçiliyse işlem yapma (Performans)
        if (_currentCulture == cultureCode) return;

        try
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            var uri = new Uri($"{LangPathPart}{cultureCode}.xaml", UriKind.Relative);
            var newDict = new ResourceDictionary { Source = uri };

            if (Application.Current?.Resources == null) return;

            var appResources = Application.Current.Resources.MergedDictionaries;

            // 2. Eski dil dosyasını bul ve güvenli bir şekilde kaldır
            var oldDict = appResources.FirstOrDefault(d =>
                d.Source != null &&
                d.Source.OriginalString.Contains(LangPathPart));

            if (oldDict != null)
            {
                appResources.Remove(oldDict);
            }

            // 3. Yeni dil dosyasını ekle
            appResources.Add(newDict);
            _currentCulture = cultureCode;

            // Loglama (Opsiyonel: ILogger entegrasyonu yoksa Debug kalabilir)
            System.Diagnostics.Debug.WriteLine($"Dil başarıyla değiştirildi: {cultureCode}");
        }
        catch (Exception ex)
        {
            // Burada kurumsal bir hata yönetimi veya loglama servisi kullanılmalı
            System.Diagnostics.Debug.WriteLine($"Localization Error: {ex.Message}");
        }
    }
}