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

public sealed class RegexTesterViewModelTests
{
    private sealed class FakeService : IRegexTesterService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public int MaxMatchCount => 10_000;
        public TimeSpan RegexTimeout => TimeSpan.FromSeconds(2);
        public ToolResult<RegexReport>? Result { get; set; }

        public Task<ToolResult<RegexReport>> TestAsync(string? pattern, string? input,
            RegexTestOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<RegexReport>.Ok(
                new RegexReport(
                    new List<RegexMatch> { new(0, 1, "a", new[] { "a" }) },
                    "[a]bc", "", 1, TimeSpan.Zero)));

        public IReadOnlyList<RegexSample> GetSamples()
            => new[] { new RegexSample("Sample", @"\d+", "123", "X") };
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

    private static RegexTesterViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new RegexTesterViewModel(svc, fe, new FakeLoc(),
            NullLogger<RegexTesterViewModel>.Instance);
    }

    [Fact]
    public void Samples_Populated()
    {
        var vm = Create(out _, out _);
        Assert.NotEmpty(vm.Samples);
    }

    [Fact]
    public void LimitsText_ContainsValues()
    {
        var vm = Create(out _, out _);
        Assert.Contains("10", vm.LimitsText);
    }

    [Fact]
    public async Task TestCommand_WithSuccess_PopulatesMatches()
    {
        var vm = Create(out _, out _);
        vm.Pattern = @"\d+";
        vm.Input = "123";
        await vm.TestCommand.ExecuteAsync(null);
        Assert.Single(vm.Matches);
        Assert.Equal(1, vm.MatchCount);
    }

    [Fact]
    public async Task TestCommand_WithFailure_Clears()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<RegexReport>.Fail(ToolErrorKind.InvalidFormat, "x", "err");
        vm.Pattern = "[";
        vm.Input = "abc";
        await vm.TestCommand.ExecuteAsync(null);
        Assert.Empty(vm.Matches);
        Assert.Equal(0, vm.MatchCount);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.Pattern = "x";
        vm.Input = "y";
        vm.Highlighted = "z";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.Pattern);
        Assert.Empty(vm.Input);
        Assert.Empty(vm.Highlighted);
        Assert.Empty(vm.Matches);
    }
}