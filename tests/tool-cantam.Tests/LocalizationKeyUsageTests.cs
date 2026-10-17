using System.IO;
using System.Text.RegularExpressions;
using tool_cantam.Helpers;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Görünümlerde ve ViewModel'lerde kullanılan yerelleştirme anahtarlarının iki dil dosyasında da
/// bulunduğunu doğrular. Eksik anahtar çalışma zamanında ekranda anahtar adı olarak görünür;
/// bu test onu derleme sonrası yakalar.
/// </summary>
public class LocalizationKeyUsageTests
{
    private static readonly Regex ViewKeyPattern = new(@"\{DynamicResource\s+(L\.[A-Za-z0-9_.]+)\}", RegexOptions.Compiled);

    private static readonly Regex ViewModelKeyPattern = new("\"(L\\.[A-Za-z0-9_.]+)\"", RegexOptions.Compiled);

    [Fact]
    public void Every_key_referenced_by_a_view_exists_in_both_languages()
    {
        string sourceRoot = Path.Combine(AppContext.BaseDirectory, "ApplicationSources", "Views");
        Assert.True(Directory.Exists(sourceRoot), $"Kaynak görünüm klasörü bulunamadı: {sourceRoot}");

        var referenced = Collect(sourceRoot, "*.xaml", ViewKeyPattern);
        Assert.NotEmpty(referenced);

        AssertKeysExist(referenced, "görünüm");
    }

    [Fact]
    public void Every_key_referenced_by_a_view_model_exists_in_both_languages()
    {
        string sourceRoot = Path.Combine(AppContext.BaseDirectory, "ApplicationSources", "ViewModels");
        Assert.True(Directory.Exists(sourceRoot), $"Kaynak ViewModel klasörü bulunamadı: {sourceRoot}");

        var referenced = Collect(sourceRoot, "*.cs", ViewModelKeyPattern);
        Assert.NotEmpty(referenced);

        AssertKeysExist(referenced, "ViewModel");
    }

    private static void AssertKeysExist(IReadOnlyCollection<string> referenced, string scope)
    {
        foreach (string culture in LocalizationService.SupportedCultureCodes)
        {
            HashSet<string> defined = LanguageResourceFiles.LoadKeys(culture);
            string[] missing = referenced.Except(defined, StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToArray();

            Assert.True(missing.Length == 0,
                $"{culture} dosyasında eksik {missing.Length} anahtar var ({scope}): {string.Join(", ", missing)}");
        }
    }

    private static string[] Collect(string root, string pattern, Regex keyPattern) =>
        Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .SelectMany(file => keyPattern.Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
