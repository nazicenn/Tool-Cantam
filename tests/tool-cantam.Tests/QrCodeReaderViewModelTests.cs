using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class QrCodeReaderViewModelTests
{
    private sealed class FakeReader : IQrCodeReaderService
    {
        public long MaxFileSize => 20L * 1024 * 1024;
        public System.Collections.Generic.IReadOnlyList<string> SupportedExtensions => new[] { ".png" };
        public ToolResult<QrCodeReadReport>? Result { get; set; }

        public Task<ToolResult<QrCodeReadReport>> ReadAsync(string? p, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<QrCodeReadReport>.Ok(
                new QrCodeReadReport(p ?? "", "hi", "QR_CODE", 0)));
    }

    private sealed class FakeFe : IFileExportService
    {
        public string? OpenPath { get; set; }
        public string? LastCopied { get; private set; }
        public string? RequestOpenPath(string f, string t) => OpenPath;
        public string? RequestSavePath(string f, string e, string n) => null;
        public Task<bool> ExportTextAsync(string t, string p, string f, string ti) => Task.FromResult(true);
        public void CopyToClipboard(string t) => LastCopied = t;
    }

    private sealed class FakeLoc : ILocalizerService
    {
        private string _c = "tr-TR";
        public string CurrentCulture => _c;
        public System.Collections.Generic.IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };
        public string Get(string k) => k;
        public string Format(string k, params object?[] a)
            => a is { Length: > 0 } ? $"{k} [{string.Join(", ", a)}]" : k;
        public bool SetCulture(string c) { _c = c; return true; }
    }

    private static QrCodeReaderViewModel Create(out FakeReader svc, out FakeFe fe)
    {
        svc = new FakeReader();
        fe = new FakeFe();
        return new QrCodeReaderViewModel(svc, fe, new FakeLoc(), NullLogger<QrCodeReaderViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsMax()
    {
        var vm = Create(out _, out _);
        Assert.Contains("20", vm.LimitsText);
    }

    [Fact]
    public void SelectFile_SetsPath()
    {
        var vm = Create(out _, out var fe);
        fe.OpenPath = "x.png";
        vm.SelectFileCommand.Execute(null);
        Assert.Equal("x.png", vm.FilePath);
    }

    [Fact]
    public async Task ReadCommand_WithSuccess_SetsText()
    {
        var vm = Create(out _, out _);
        var tmp = Path.Combine(Path.GetTempPath(), $"qr_vm_{System.Guid.NewGuid():N}.png");
        File.WriteAllBytes(tmp, new byte[] { 1 });
        try
        {
            vm.FilePath = tmp;
            await vm.ReadCommand.ExecuteAsync(null);
            Assert.Equal("hi", vm.QrText);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void CopyResult_WithValue_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.QrText = "result";
        vm.CopyResultCommand.Execute(null);
        Assert.Equal("result", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.FilePath = "x";
        vm.QrText = "y";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.FilePath);
        Assert.Empty(vm.QrText);
    }
}