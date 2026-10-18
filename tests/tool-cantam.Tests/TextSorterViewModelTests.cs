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

public sealed class TextSorterViewModelTests
{
    private sealed class FakeService : ITextSorterService
    {
        public int MaxTextLength => 10 * 1024 * 1024;
        public ToolResult<TextSorterReport>? Result { get; set; }

        public Task<ToolResult<TextSorterReport>> SortAsync(
            string? input, TextSorterOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<TextSorterReport>.Ok(
                new TextSorterReport("x", 1, 1, 0, 0, TimeSpan.Zero)));
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

    private static TextSorterViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new TextSorterViewModel(svc, fe, new FakeLoc(),
            NullLogger<TextSorterViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimit()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
    }

    [Fact]
    public async Task SortCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        vm.InputText = "x";
        await vm.SortCommand.ExecuteAsync(null);
        Assert.Equal("x", vm.OutputText);
    }

    [Fact]
    public async Task SortCommand_WithFailure_ClearsOutput()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<TextSorterReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.InputText = "x";
        await vm.SortCommand.ExecuteAsync(null);
        Assert.Empty(vm.OutputText);
    }

    [Fact]
    public void CopyOutput_WithValue_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.OutputText = "hello";
        vm.CopyOutputCommand.Execute(null);
        Assert.Equal("hello", fe.LastCopied);
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