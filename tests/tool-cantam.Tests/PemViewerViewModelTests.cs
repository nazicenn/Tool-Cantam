using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class PemViewerViewModelTests
{
    private sealed class FakeSvc : IPemViewerService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<PemReport>? Result { get; set; }
        public Task<ToolResult<PemReport>> ParseAsync(string? i, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<PemReport>.Ok(Sample()));
        private static PemReport Sample() => new(
            "CN=subject", "CN=issuer", "12345",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(365), true,
            "RSA", "SHA256", "ABC",
            new List<PemBlock> { new("CERTIFICATE", 800) }, TimeSpan.Zero);
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
    private static PemViewerViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new PemViewerViewModel(svc, fe, new FakeLoc(), NullLogger<PemViewerViewModel>.Instance);
    }

    [Fact] public async Task Parse_Success() { var vm = Create(out _, out _); vm.PemInput = "pem"; await vm.ParseCommand.ExecuteAsync(null); Assert.Equal("CN=subject", vm.Subject); Assert.Single(vm.Blocks); }
    [Fact] public async Task Parse_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<PemReport>.Fail(ToolErrorKind.InvalidFormat, "x", "e"); vm.PemInput = "x"; await vm.ParseCommand.ExecuteAsync(null); Assert.Empty(vm.Subject); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.Subject = "x"; vm.CopyAllCommand.Execute(null); Assert.NotNull(fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Subject = "x"; vm.PemInput = "p"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Subject); Assert.Empty(vm.PemInput); }
}