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

public sealed class CorsAnalyzerViewModelTests
{
    private sealed class FakeSvc : ICorsAnalyzerService
    {
        public int MaxInputLength => 100 * 1024;
        public ToolResult<CorsReport>? Result { get; set; }
        public Task<ToolResult<CorsReport>> AnalyzeAsync(string? h, CorsOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<CorsReport>.Ok(Sample()));
        public Task<ToolResult<CorsReport>> FetchAndAnalyzeAsync(string? u, CorsOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<CorsReport>.Ok(Sample()));
        private static CorsReport Sample() => new(
            "input",
            new List<CorsHeader> { new("Access-Control-Allow-Origin", "*", CorsHeaderKind.AllowOrigin) },
            new List<CorsIssue> { new("wildcard", "Warning", CorsSeverity.Warning) },
            true, "Warnings", TimeSpan.Zero);
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
    private static CorsAnalyzerViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new CorsAnalyzerViewModel(svc, fe, new FakeLoc(), NullLogger<CorsAnalyzerViewModel>.Instance);
    }

    [Fact] public void Constructor_Ready() { var vm = Create(out _, out _); Assert.NotNull(vm.StatusText); }
    [Fact] public async Task Analyze_Success() { var vm = Create(out _, out _); vm.Headers = "test"; await vm.AnalyzeCommand.ExecuteAsync(null); Assert.Single(vm.ParsedHeaders); }
    [Fact] public async Task Analyze_Fail_Clears() { var vm = Create(out var s, out _); s.Result = ToolResult<CorsReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.Headers = "test"; await vm.AnalyzeCommand.ExecuteAsync(null); Assert.Empty(vm.ParsedHeaders); }
    [Fact] public void SwitchToUrl_Works() { var vm = Create(out _, out _); vm.SwitchToUrlCommand.Execute(null); Assert.True(vm.IsFromUrl); }
    [Fact] public void Clear_Resets() { var vm = Create(out _, out _); vm.Headers = "x"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Headers); }
}