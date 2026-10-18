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

public sealed class SlugGeneratorViewModelTests
{
    private sealed class FakeService : ISlugGeneratorService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public int MaxSlugLength => 200;
        public ToolResult<SlugReport>? Result { get; set; }

        public Task<ToolResult<SlugReport>> GenerateAsync(
            string? input, SlugOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<SlugReport>.Ok(
                new SlugReport("hello-world", 11, 11, TimeSpan.Zero)));
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

    private static SlugGeneratorViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new SlugGeneratorViewModel(svc, fe, new FakeLoc(),
            NullLogger<SlugGeneratorViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimits()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
        Assert.Contains("200", vm.LimitsText);
    }

    [Fact]
    public async Task GenerateCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        vm.InputText = "hello";
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Equal("hello-world", vm.OutputText);
    }

    [Fact]
    public async Task GenerateCommand_WithFailure_ClearsOutput()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<SlugReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.InputText = "x";
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Empty(vm.OutputText);
    }

    [Fact]
    public void CopyOutput_WithValue_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.OutputText = "hello-world";
        vm.CopyOutputCommand.Execute(null);
        Assert.Equal("hello-world", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.InputText = "x";
        vm.OutputText = "y";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.InputText);
        Assert.Empty(vm.OutputText);
    }
}