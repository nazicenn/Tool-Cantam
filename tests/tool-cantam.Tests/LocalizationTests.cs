using System.Windows;
using tool_cantam.Helpers;
using tool_cantam.Services;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Dil altyapısını kilitler: ayar dosyasındaki eski "English"/"Türkçe" değerleri çalışma zamanındaki
/// en-US/tr-TR kaynaklarına bağlanmalı ve iki dil dosyası aynı anahtar kümesini sunmalıdır.
/// </summary>
public class LocalizationTests
{
    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("en", "en-US")]
    [InlineData("English", "en-US")]
    [InlineData("ENGLISH", "en-US")]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("tr", "tr-TR")]
    [InlineData("Türkçe", "tr-TR")]
    [InlineData("Turkce", "tr-TR")]
    [InlineData("  tr-TR  ", "tr-TR")]
    public void Known_language_values_map_to_supported_culture_codes(string input, string expected)
    {
        Assert.True(LocalizationService.TryNormalizeCulture(input, out string culture));
        Assert.Equal(expected, culture);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("de-DE")]
    [InlineData("Klingon")]
    public void Unknown_language_values_are_rejected(string? input)
    {
        Assert.False(LocalizationService.TryNormalizeCulture(input, out string culture));
        Assert.Equal(LocalizationService.DefaultCultureCode, culture);
    }

    [Fact]
    public void Every_supported_culture_loads_a_resource_dictionary()
    {
        foreach (string culture in LocalizationService.SupportedCultureCodes)
        {
            ResourceDictionary dictionary = LoadLanguageDictionary(culture);
            Assert.NotEmpty(dictionary.Keys);
        }
    }

    [Fact]
    public void Language_files_expose_the_same_keys()
    {
        var english = ToSortedKeys(LoadLanguageDictionary("en-US"));
        var turkish = ToSortedKeys(LoadLanguageDictionary("tr-TR"));

        string[] onlyInEnglish = english.Except(turkish, StringComparer.Ordinal).ToArray();
        string[] onlyInTurkish = turkish.Except(english, StringComparer.Ordinal).ToArray();

        Assert.True(onlyInEnglish.Length == 0, "Yalnızca en-US dosyasında olan anahtarlar: " + string.Join(", ", onlyInEnglish));
        Assert.True(onlyInTurkish.Length == 0, "Yalnızca tr-TR dosyasında olan anahtarlar: " + string.Join(", ", onlyInTurkish));
    }

    [Fact]
    public void Localizer_returns_the_key_when_no_resource_host_is_available()
    {
        var localizer = new LocalizerService();

        // Birim testlerinde WPF Application yoktur; çözümleyici anahtarı döndürmeli, boş metin değil.
        Assert.Equal("L.Base64.Title", localizer.Get("L.Base64.Title"));
        Assert.Equal("L.Base64.Status.Encoded", localizer.Format("L.Base64.Status.Encoded", 1, 2));
    }

    [Fact]
    public void Localizer_survives_a_template_without_placeholders()
    {
        var localizer = new LocalizerService();

        string result = localizer.Format("L.Base64.Title", 1, 2);

        Assert.Equal("L.Base64.Title", result);
    }

    private static ResourceDictionary LoadLanguageDictionary(string culture) =>
        // Dil dosyaları uygulama derlemesine BAML olarak gömülür; bu yüzden derleme adı açıkça verilir.
        new() { Source = new Uri($"pack://application:,,,/tool-cantam;component/Resources/Languages/{culture}.xaml", UriKind.Absolute) };

    private static string[] ToSortedKeys(ResourceDictionary dictionary) =>
        dictionary.Keys.Cast<object>()
            .Select(key => key.ToString() ?? string.Empty)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
}
