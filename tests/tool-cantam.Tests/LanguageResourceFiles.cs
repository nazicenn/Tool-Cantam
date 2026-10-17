using System.IO;
using System.Xml.Linq;

namespace ToolCantam.Tests;

/// <summary>
/// Dil dosyalarını (kaynak XAML) doğrudan okur.
/// <para>
/// Testler WPF'in pack URI kaynak yüklemesini kullanmaz: yeni derlenmiş bir derlemeden BAML okumak
/// antivirüs taraması gibi dış etkenlerle tek seferlik başarısız olabiliyor ve bu da kararsız test
/// üretiyor. Burada depodaki gerçek kaynak dosyalar okunur; hem daha sağlam hem de sürülen dosyanın
/// kendisini doğrular.
/// </para>
/// </summary>
internal static class LanguageResourceFiles
{
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>Testlerin bildiği dil kodları.</summary>
    public static readonly string[] Cultures = { "en-US", "tr-TR" };

    public static string GetPath(string culture) =>
        Path.Combine(AppContext.BaseDirectory, "ApplicationSources", "Languages", culture + ".xaml");

    public static bool Exists(string culture) => File.Exists(GetPath(culture));

    /// <summary>Dil dosyasındaki anahtar kümesini döndürür.</summary>
    public static HashSet<string> LoadKeys(string culture)
    {
        XDocument document = XDocument.Load(GetPath(culture));

        return document.Root!
            .Elements()
            .Select(element => element.Attribute(XamlNamespace + "Key")?.Value)
            .Where(key => !string.IsNullOrEmpty(key))
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
