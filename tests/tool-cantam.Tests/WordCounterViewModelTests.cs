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

public sealed class WordCounterViewModelTests
{
    private sealed class FakeSvc : IWordCounterService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<WordCountReport>? Result { get; set; }
        public Task<ToolResult<WordCountReport>> AnalyzeAsync(string? input, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<WordCountReport>.Ok(new WordCountReport(
                100, 80, 20, 3, 1, 2,
                TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9),
                new List<WordCounterWordFreq> { new("test", 5) }, TimeSpan.Zero)));
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
    private static WordCounterViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new WordCounterViewModel(svc, fe, new FakeLoc(), NullLogger<WordCounterViewModel>.Instance);
    }

    [Fact]
    public async Task Input_TriggersAnalyze_PopulatesStats()
    {
        var vm = Create(out _, out _);
        vm.InputText = "test";
        await Task.Delay(400, TestContext.Current.CancellationToken); // debounce
        Assert.Equal(20, vm.WordCount);
        Assert.Equal(100, vm.CharacterCount);
    }

    [Fact]
    public void CopyStatistics_NoData_Warns()
    {
        var vm = Create(out _, out _);
        vm.CopyStatisticsCommand.Execute(null);
        Assert.Equal(0, vm.WordCount);
    }

    [Fact]
    public void CopyText_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.InputText = "hello";
        vm.CopyTextCommand.Execute(null);
        Assert.Equal("hello", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.InputText = "x";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.InputText);
    }
}