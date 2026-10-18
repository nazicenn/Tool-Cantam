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

public sealed class TextDiffViewModelTests
{
    private sealed class FakeService : ITextDiffService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<DiffReport>? Result { get; set; }

        public Task<ToolResult<DiffReport>> CompareAsync(string? left, string? right,
            DiffOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<DiffReport>.Ok(new DiffReport(
                new List<DiffLine>
                {
                    new(DiffLineKind.Unchanged, "a", "a", 0, 0),
                    new(DiffLineKind.Added, null, "b", -1, 1),
                },
                1, 0, 0, 1, TimeSpan.Zero)));
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

    private static TextDiffViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new TextDiffViewModel(svc, fe, new FakeLoc(),
            NullLogger<TextDiffViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimit()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
    }

    [Fact]
    public async Task CompareCommand_WithSuccess_PopulatesResult()
    {
        var vm = Create(out _, out _);
        vm.LeftText = "a";
        vm.RightText = "b";
        await vm.CompareCommand.ExecuteAsync(null);
        Assert.Equal(2, vm.DiffLines.Count);
        Assert.Equal(1, vm.AddedLines);
        Assert.Equal(1, vm.UnchangedLines);
    }

    [Fact]
    public async Task CompareCommand_WithFailure_Clears()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<DiffReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.LeftText = "a";
        vm.RightText = "b";
        await vm.CompareCommand.ExecuteAsync(null);
        Assert.Empty(vm.DiffLines);
        Assert.Equal(0, vm.AddedLines);
    }

    [Fact]
    public void Swap_SwapsInputs()
    {
        var vm = Create(out _, out _);
        vm.LeftText = "A";
        vm.RightText = "B";
        vm.SwapCommand.Execute(null);
        Assert.Equal("B", vm.LeftText);
        Assert.Equal("A", vm.RightText);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.LeftText = "a";
        vm.RightText = "b";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.LeftText);
        Assert.Empty(vm.RightText);
        Assert.Empty(vm.DiffLines);
    }
}