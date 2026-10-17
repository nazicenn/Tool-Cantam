using tool_cantam.Core.Contracts;
using tool_cantam.Services.Interfaces;

namespace ToolCantam.Tests;

/// <summary>
/// Arayüz metinlerini anahtara indirgeyen sahte çözümleyici; testler sabit metne bağlı kalmaz.
/// Gerçek anahtarların ve çevirilerin doğruluğu ayrıca LocalizationService testlerinde kontrol edilir.
/// </summary>
internal sealed class FakeLocalizerService : ILocalizerService
{
    public string CurrentCulture { get; private set; } = "en-US";

    public IReadOnlyCollection<string> AvailableCultures { get; } = new[] { "en-US", "tr-TR" };

    public string Get(string key) => key;

    public string Format(string key, params object?[] args) =>
        args.Length == 0 ? key : $"{key}|{string.Join("|", args)}";

    public bool SetCulture(string cultureCode)
    {
        CurrentCulture = cultureCode;
        return true;
    }
}

/// <summary>
/// Dosya diyalogları ve pano erişimini taklit eder; hata yolları da test edilebilir.
/// </summary>
internal sealed class FakeFileExportService : IFileExportService
{
    public string? ClipboardContent { get; private set; }

    public bool ClipboardThrows { get; set; }

    public string? NextOpenPath { get; set; }

    public string? NextSavePath { get; set; }

    public string? ExportedContent { get; private set; }

    public bool ExportResult { get; set; } = true;

    public string? RequestSavePath(string filter, string defaultExt, string defaultFileName = "") => NextSavePath;

    public string? RequestOpenPath(string filter, string title = "Select File") => NextOpenPath;

    public void CopyToClipboard(string content)
    {
        if (ClipboardThrows)
            throw new InvalidOperationException("Simulated clipboard contention.");

        ClipboardContent = content;
    }

    public Task<bool> ExportTextAsync(string content, string filter, string defaultExt, string defaultFileName = "Export")
    {
        ExportedContent = content;
        return Task.FromResult(ExportResult);
    }
}
