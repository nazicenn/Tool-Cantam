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

public sealed class Base64ImageConverterViewModelTests
{
    // Geçerli 1x1 PNG byte'ları
    private static readonly byte[] MinimalPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

    private sealed class FakeSvc : IBase64ImageService
    {
        public long MaxFileSize => 50L * 1024 * 1024;
        public ToolResult<ImageToBase64Report>? ImgResult { get; set; }
        public ToolResult<Base64ToImageReport>? B64Result { get; set; }
        public Task<ToolResult<ImageToBase64Report>> ImageToBase64Async(string? p, CancellationToken ct)
            => Task.FromResult(ImgResult ?? ToolResult<ImageToBase64Report>.Ok(new ImageToBase64Report("AAAA", 100, "PNG", TimeSpan.Zero)));
        public Task<ToolResult<Base64ToImageReport>> Base64ToImageAsync(string? b, CancellationToken ct)
            => Task.FromResult(B64Result ?? ToolResult<Base64ToImageReport>.Ok(new Base64ToImageReport(MinimalPng, "PNG", MinimalPng.Length)));
    }
    private sealed class FakeFe : IFileExportService
    {
        public string? LastCopied { get; private set; }
        public string? SavePath { get; set; }
        public string? OpenPath { get; set; }
        public string? RequestOpenPath(string f, string t = "Select File") => OpenPath;
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
    private static Base64ImageConverterViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new Base64ImageConverterViewModel(svc, fe, new FakeLoc(), NullLogger<Base64ImageConverterViewModel>.Instance);
    }

    [Fact] public void FilePathDisplay_NoFile_Localized() { var vm = Create(out _, out _); Assert.NotNull(vm.FilePathDisplay); }
    [Fact] public async Task ConvertImg_Success() { var vm = Create(out _, out _); var tmp = Path.Combine(Path.GetTempPath(), $"b64_{Guid.NewGuid():N}.png"); File.WriteAllBytes(tmp, new byte[] { 1, 2, 3 }); try { vm.FilePath = tmp; await vm.ConvertImageToBase64Command.ExecuteAsync(null); Assert.Equal("AAAA", vm.Base64Text); } finally { File.Delete(tmp); } }
    [Fact] public async Task ConvertImg_Fail() { var vm = Create(out var s, out _); s.ImgResult = ToolResult<ImageToBase64Report>.Fail(ToolErrorKind.Validation, "x", "e"); var tmp = Path.Combine(Path.GetTempPath(), $"b64_{Guid.NewGuid():N}.png"); File.WriteAllBytes(tmp, new byte[] { 1 }); try { vm.FilePath = tmp; await vm.ConvertImageToBase64Command.ExecuteAsync(null); Assert.Empty(vm.Base64Text); } finally { File.Delete(tmp); } }
    [Fact] public async Task ConvertB64_Success() { var vm = Create(out _, out _); vm.Base64Text = "AAAA"; await vm.ConvertBase64ToImageCommand.ExecuteAsync(null); Assert.NotNull(vm.PreviewImage); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.Base64Text = "x"; vm.CopyBase64Command.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Base64Text = "x"; vm.FilePath = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Base64Text); Assert.Empty(vm.FilePath); }
}