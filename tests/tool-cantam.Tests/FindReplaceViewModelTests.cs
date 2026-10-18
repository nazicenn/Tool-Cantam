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

public sealed class FindReplaceViewModelTests
{
    private sealed class FakeService : IFindReplaceService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<FindReplaceReport>? Result { get; set; }

        public Task<ToolResult<FindReplaceReport>> ReplaceAsync(
            string? input, string? find, string? replace,
            FindReplaceOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<FindReplaceReport>.Ok(
                new FindReplaceReport("out", 2, 2, "x", TimeSpan.Zero)));
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

    private static FindReplaceViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new FindReplaceViewModel(svc, fe, new FakeLoc(),
            NullLogger<FindReplaceViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimit()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
    }

    [Fact]
    public async Task ReplaceCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        vm.InputText = "x";
        vm.FindText = "x";
        await vm.ReplaceCommand.ExecuteAsync(null);
        Assert.Equal("out", vm.OutputText);
        Assert.Equal(2, vm.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceCommand_WithFailure_ClearsOutput()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<FindReplaceReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.InputText = "x";
        vm.FindText = "x";
        await vm.ReplaceCommand.ExecuteAsync(null);
        Assert.Empty(vm.OutputText);
        Assert.Equal(0, vm.ReplaceCount);
    }

    [Fact]
    public void Swap_SwapsInputOutput()
    {
        var vm = Create(out _, out _);
        vm.InputText = "A";
        vm.OutputText = "B";
        vm.SwapCommand.Execute(null);
        Assert.Equal("B", vm.InputText);
        Assert.Equal("A", vm.OutputText);
        Assert.Equal(0, vm.ReplaceCount);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.InputText = "x";
        vm.FindText = "y";
        vm.ReplaceText = "z";
        vm.OutputText = "o";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.InputText);
        Assert.Empty(vm.FindText);
        Assert.Empty(vm.ReplaceText);
        Assert.Empty(vm.OutputText);
    }
}