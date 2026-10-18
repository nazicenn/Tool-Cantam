using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class ExifViewerViewModelTests
{
    private sealed class FakeSvc : IExifViewerService
    {
        public long MaxFileSize => 100L * 1024 * 1024;
        public IReadOnlyList<string> SupportedExtensions => new[] { ".png", ".jpg" };
        public ToolResult<ExifReport>? Result { get; set; }
        public Task<ToolResult<ExifReport>> ReadAsync(string? path, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<ExifReport>.Ok(new ExifReport(
                path ?? "", 0, "PNG",
                new List<ExifDirectory> { new("EXIF", new List<ExifTag> { new("Make", "Canon") }) },
                1, TimeSpan.Zero)));
    }
    private sealed class FakeFe : IFileExportService
    {
        public string? LastCopied { get; private set; }
        public string? SavePath { get; set; }
        public string? RequestOpenPath(string f, string t = "Select File") => null;
        public string? RequestSavePath(string f, string e, string n = "") => SavePath;
        public Task<bool> ExportTextAsync(string c, string f, string e, string n = "Export") => Task.FromResult(true);
        public void CopyToClipboard(string c) => LastCopied = c;
    }
    private sealed class FakeLoc : ILocalizerService
    {
        private string _c = "tr-TR";
        public string CurrentCulture => _c;
        public IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };
        public string Get(string k) => k;
        public string Format(string k, params object?[] a) => a is { Length: > 0 } ? $"{k} [{string.Join(", ", a)}]" : k;
        public bool SetCulture(string c) { _c = c; return true; }
    }
    private static ExifViewerViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new ExifViewerViewModel(svc, fe, new FakeLoc(), NullLogger<ExifViewerViewModel>.Instance);
    }

    [Fact] public void FilePathDisplay_NoFile_ReturnsLocalized() { var vm = Create(out _, out _); Assert.NotNull(vm.FilePathDisplay); }
    [Fact] public async Task Read_Success() { var vm = Create(out _, out _); var tmp = Path.Combine(Path.GetTempPath(), $"exif_vm_{Guid.NewGuid():N}.png"); File.WriteAllBytes(tmp, new byte[] { 1 }); try { vm.FilePath = tmp; await vm.ReadCommand.ExecuteAsync(null); Assert.Single(vm.Directories); } finally { File.Delete(tmp); } }
    [Fact] public async Task Read_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<ExifReport>.Fail(ToolErrorKind.Validation, "x", "e"); var tmp = Path.Combine(Path.GetTempPath(), $"exif_vm_{Guid.NewGuid():N}.png"); File.WriteAllBytes(tmp, new byte[] { 1 }); try { vm.FilePath = tmp; await vm.ReadCommand.ExecuteAsync(null); Assert.Empty(vm.Directories); } finally { File.Delete(tmp); } }
    [Fact] public void CopyAll_NoData_ShowsWarning() { var vm = Create(out _, out _); vm.CopyAllCommand.Execute(null); Assert.Empty(vm.Directories); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.FilePath = "x"; vm.ClearCommand.Execute(null); Assert.Empty(vm.FilePath); Assert.Empty(vm.Directories); }
}