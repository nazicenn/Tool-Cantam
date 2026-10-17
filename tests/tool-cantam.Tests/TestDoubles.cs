using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
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

/// <summary>
/// Ağa çıkmayan port tarayıcı taklidi: doğrulama ve sunum davranışını deterministik test eder.
/// </summary>
internal sealed class FakePortScannerService : IPortScannerService
{
    public int MaxPortsPerScan { get; init; } = 4096;

    public int MaxConcurrency { get; init; } = 50;

    public int MaxTimeoutMs { get; init; } = 5000;

    /// <summary>Bir sonraki çağrıda döndürülecek sonuç; null ise beklenmeyen hata döner.</summary>
    public ToolResult<PortScanReport>? NextResult { get; set; }

    public int ScanCount { get; private set; }

    public int LastStartPort { get; private set; }

    public int LastEndPort { get; private set; }

    public int LastTimeoutMs { get; private set; }

    public Task<ToolResult<PortScanReport>> ScanAsync(
        string? target,
        int startPort,
        int endPort,
        int timeoutMs,
        IProgress<PortScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ScanCount++;
        LastStartPort = startPort;
        LastEndPort = endPort;
        LastTimeoutMs = timeoutMs;

        int total = endPort - startPort + 1;
        progress?.Report(new PortScanProgress(total, total, NextResult?.Value?.OpenCount ?? 0));

        return Task.FromResult(NextResult ?? ToolResult<PortScanReport>.Fail(
            ToolErrorKind.Unexpected, "L.Common.Error.Unexpected", "No result configured for the fake scanner."));
    }

    public static PortScanReport Report(string target, int startPort, int endPort, params int[] openPorts)
    {
        var entries = new List<PortScanEntry>();
        foreach (int port in openPorts)
            entries.Add(new PortScanEntry(port, tool_cantam.Core.Net.WellKnownPorts.GetServiceName(port)));

        int total = endPort - startPort + 1;

        return new PortScanReport(
            target,
            System.Net.IPAddress.Parse("127.0.0.1"),
            startPort,
            endPort,
            total,
            entries.Count,
            total - entries.Count,
            0,
            TimeSpan.FromSeconds(1.5),
            entries);
    }
}
