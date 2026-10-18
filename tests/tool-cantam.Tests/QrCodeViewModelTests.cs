using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class QrCodeViewModelTests
{
    private sealed class FakeQrService : IQrCodeService
    {
        public int MaxInputLength => 2048;
        public ToolResult<QrCodeReport>? Result { get; set; }

        // Geçerli 1x1 saydam PNG — BitmapImage decode edebilsin
        private static readonly byte[] MinimalPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        public Task<ToolResult<QrCodeReport>> GenerateAsync(
            string? input, QrCodeEcc ecc, int px, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<QrCodeReport>.Ok(
                new QrCodeReport(input ?? "", ecc, px, MinimalPng, MinimalPng.Length)));
    }

    private sealed class FakeFe : IFileExportService
    {
        public string? SavePath { get; set; }
        public string? OpenPath { get; set; }

        public string? RequestOpenPath(string filter, string title = "Select File") => OpenPath;

        public string? RequestSavePath(string filter, string defaultExt, string defaultFileName = "") => SavePath;

        public Task<bool> ExportTextAsync(string content, string filter, string defaultExt, string defaultFileName = "Export")
            => Task.FromResult(true);

        public void CopyToClipboard(string content) { }
    }

    private sealed class FakeLoc : ILocalizerService
    {
        private string _c = "tr-TR";
        public string CurrentCulture => _c;
        public IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };
        public string Get(string k) => k;
        public string Format(string k, params object?[] a)
            => a is { Length: > 0 } ? $"{k} [{string.Join(", ", a)}]" : k;
        public bool SetCulture(string c) { _c = c; return true; }
    }

    private static QrCodeViewModel Create(out FakeQrService svc)
    {
        svc = new FakeQrService();
        return new QrCodeViewModel(svc, new FakeFe(), new FakeLoc(),
            NullLogger<QrCodeViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsMax()
    {
        var vm = Create(out _);
        Assert.Contains("2048", vm.LimitsText);
    }

    [Fact]
    public async Task GenerateCommand_WithSuccess_SetsImage()
    {
        var vm = Create(out _);
        vm.InputText = "hello";
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.NotNull(vm.QrCodeImage);
    }

    [Fact]
    public async Task GenerateCommand_WithFailure_NoImage()
    {
        var vm = Create(out var svc);
        svc.Result = ToolResult<QrCodeReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.InputText = "";
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Null(vm.QrCodeImage);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _);
        vm.InputText = "x";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.InputText);
        Assert.Null(vm.QrCodeImage);
    }
}