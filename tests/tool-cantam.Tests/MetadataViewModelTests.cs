using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class MetadataViewModelTests
{
    // -------- Fake IMetadataService --------
    private sealed class FakeMetadataService : IMetadataService
    {
        public long MaxFileSize => 100L * 1024 * 1024;
        public IReadOnlyList<string> SupportedExtensions => new[] { ".png", ".jpg" };

        public ToolResult<MetadataReport>? AnalyzeResult { get; set; }
        public ToolResult<CleanReport>? CleanResult { get; set; }

        public Task<ToolResult<MetadataReport>> AnalyzeAsync(string? path, CancellationToken ct)
            => Task.FromResult(AnalyzeResult ?? ToolResult<MetadataReport>.Ok(
                new MetadataReport(path ?? "", 0, "PNG",
                    Array.Empty<MetadataEntry>(),
                    false, false, false, false, false, false)));

        public Task<ToolResult<CleanReport>> CleanAsync(string? src, string? dst,
            MetadataCleanOptions o, CancellationToken ct)
            => Task.FromResult(CleanResult ?? ToolResult<CleanReport>.Ok(
                new CleanReport(0, 0, 0, TimeSpan.Zero)));
    }

    // -------- Fake IFileExportService (tam imza) --------
    private sealed class FakeFileExportService : IFileExportService
    {
        public string? OpenPathResult { get; set; }
        public string? SavePathResult { get; set; }

        public string? RequestOpenPath(string filter, string title) => OpenPathResult;
        public string? RequestSavePath(string filter, string defaultExt, string fileName) => SavePathResult;

        public Task<bool> ExportTextAsync(string text, string path, string filter, string title)
            => Task.FromResult(true);

        public void CopyToClipboard(string text) { }
    }

    // -------- Fake ILocalizerService (tam imza) --------
    private sealed class FakeLocalizer : ILocalizerService
    {
        private string _culture = "tr-TR";

        public string CurrentCulture => _culture;
        public IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };

        public string Get(string key) => key;

        // args'ı yerine koy — {0}, {1} gibi placeholder'ları değiştir
        public string Format(string key, params object?[] args)
            => args is { Length: > 0 } ? $"{key} [{string.Join(", ", args)}]" : key;

        public bool SetCulture(string culture)
        {
            _culture = culture;
            return true;
        }
    }

    private static MetadataViewModel Create(
        out FakeMetadataService svc,
        out FakeFileExportService fe)
    {
        svc = new FakeMetadataService();
        fe = new FakeFileExportService();
        return new MetadataViewModel(svc, fe, new FakeLocalizer(), NullLogger<MetadataViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimit()
    {
        var vm = Create(out _, out _);
        Assert.Contains("100", vm.LimitsText);
    }

    [Fact]
    public void SelectFile_WithPath_SetsFilePath()
    {
        var vm = Create(out _, out var fe);
        fe.OpenPathResult = "picked.png";
        vm.SelectFileCommand.Execute(null);
        Assert.Equal("picked.png", vm.FilePath);
    }

    [Fact]
    public async Task AnalyzeCommand_WithSuccess_PopulatesRows()
    {
        var vm = Create(out var svc, out _);
        svc.AnalyzeResult = ToolResult<MetadataReport>.Ok(new MetadataReport(
            "x.png", 100, "PNG",
            new[] { new MetadataEntry("EXIF", "Make", "Canon") },
            true, false, false, false, false, false));

        var tmp = Path.Combine(Path.GetTempPath(), $"vm_meta_{Guid.NewGuid():N}.png");
        File.WriteAllBytes(tmp, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        try
        {
            vm.FilePath = tmp;
            await vm.AnalyzeCommand.ExecuteAsync(null);
            Assert.Single(vm.MetadataRows);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var vm = Create(out _, out _);
        vm.FilePath = "a.png";
        vm.ClearCommand.Execute(null);
        Assert.Equal(string.Empty, vm.FilePath);
        Assert.Empty(vm.MetadataRows);
    }

    [Fact]
    public void SelectAll_ChecksAll()
    {
        var vm = Create(out _, out _);
        vm.ClearAllCommand.Execute(null);
        vm.SelectAllCommand.Execute(null);
        Assert.True(vm.CleanExif);
        Assert.True(vm.CleanGps);
        Assert.True(vm.CleanXmp);
        Assert.True(vm.CleanIptc);
        Assert.True(vm.CleanComments);
        Assert.True(vm.CleanThumbnail);
    }

    [Fact]
    public void ClearAll_UnchecksAll()
    {
        var vm = Create(out _, out _);
        vm.ClearAllCommand.Execute(null);
        Assert.False(vm.CleanExif);
        Assert.False(vm.CleanGps);
        Assert.False(vm.CleanXmp);
        Assert.False(vm.CleanIptc);
        Assert.False(vm.CleanComments);
        Assert.False(vm.CleanThumbnail);
    }
}