using System.Globalization;
using tool_cantam.Core.Contracts;
using tool_cantam.Helpers;

namespace tool_cantam.Services;

/// <summary>
/// <see cref="ILocalizerService"/> uygulaması. Kaynak sözlüğü işini
/// <see cref="LocalizationService"/> alt katmanına devreder.
/// </summary>
public sealed class LocalizerService : ILocalizerService
{
    public string CurrentCulture => LocalizationService.CurrentCulture;

    public IReadOnlyCollection<string> AvailableCultures => LocalizationService.SupportedCultureCodes;

    public string Get(string key) => LocalizationService.Get(key);

    public string Format(string key, params object?[] args)
    {
        string template = LocalizationService.Get(key);

        if (args.Length == 0)
            return template;

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            // Çeviri metnindeki yer tutucu hatası arayüzü çökertmez; ham metin gösterilir.
            return template;
        }
    }

    public bool SetCulture(string cultureCode) => LocalizationService.SetLanguage(cultureCode);
}
