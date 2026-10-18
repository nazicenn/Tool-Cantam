using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class HttpHeaderViewModelTests
{
    private sealed class FakeService : IHttpHeaderService
    {
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(10);
        public int MaxHeaders => 200;
        public ToolResult<HttpHeaderReport>? Result { get; set; }

        public bool IsValidUrl(string? url) => !string.IsNullOrWhiteSpace(url);

        public Task<ToolResult<HttpHeaderReport>> AnalyzeAsync(string? url, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<HttpHeaderReport>.Ok(new HttpHeaderReport(
                url ?? "", 200, "OK", "Server", 0,
                new List<HttpHeaderEntry> { new("X-Test", "1", HeaderCategory.Other) },
                TimeSpan.FromMilliseconds(50), false)));
    }

    private sealed class FakeFe : IFileExportService
    {
        public string? SavePath { get; set; }
        public string? LastCopied { get; private set; }
        public string? RequestOpenPath(string filter, string title = "Select File") => null;
        public string? RequestSavePath(string filter, string defaultExt, string defaultFileName = "") => SavePath;
        public Task<bool> ExportTextAsync(string content, string filter, string defaultExt, string defaultFileName = "Export") => Task.FromResult(true);
        public void CopyToClipboard(string content) => LastCopied = content;
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

    private static HttpHeaderViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new HttpHeaderViewModel(svc, fe, new FakeLoc(),
            NullLogger<HttpHeaderViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsValues()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
        Assert.Contains("200", vm.LimitsText);
    }

    [Fact]
    public async Task AnalyzeCommand_WithSuccess_PopulatesHeaders()
    {
        var vm = Create(out _, out _);
        vm.TargetUrl = "https://example.com";
        await vm.AnalyzeCommand.ExecuteAsync(null);
        Assert.Single(vm.Headers);
        Assert.Equal("X-Test", vm.Headers[0].Name);
    }

    [Fact]
    public async Task AnalyzeCommand_WithFailure_ClearsHeaders()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<HttpHeaderReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.TargetUrl = "https://example.com";
        await vm.AnalyzeCommand.ExecuteAsync(null);
        Assert.Empty(vm.Headers);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.TargetUrl = "x";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.TargetUrl);
        Assert.Empty(vm.Headers);
    }

    [Fact]
    public async Task CopyResults_WithHeaders_Copies()
    {
        var vm = Create(out _, out var fe);      // ← gerçek FakeFe'yi al
        vm.TargetUrl = "https://x";
        await vm.AnalyzeCommand.ExecuteAsync(null);
        vm.CopyResultsCommand.Execute(null);
        Assert.NotNull(fe.LastCopied);
    }
}