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

public sealed class UuidGeneratorViewModelTests
{
    private sealed class FakeService : IUuidGeneratorService
    {
        public int MinCount => 1;
        public int MaxCount => 1000;
        public ToolResult<UuidReport>? Result { get; set; }

        public Task<ToolResult<UuidReport>> GenerateAsync(UuidOptions options, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<UuidReport>.Ok(
                new UuidReport(new[] { "AAAA-1111" }, "AAAA-1111", TimeSpan.Zero, UuidVersion.V4)));
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

    private static UuidGeneratorViewModel Create(out FakeService svc, out FakeFe fe)
    {
        svc = new FakeService();
        fe = new FakeFe();
        return new UuidGeneratorViewModel(svc, fe, new FakeLoc(),
            NullLogger<UuidGeneratorViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsRange()
    {
        var vm = Create(out _, out _);
        Assert.Contains("1", vm.LimitsText);
        Assert.Contains("1000", vm.LimitsText);
    }

    [Fact]
    public async Task GenerateCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Equal("AAAA-1111", vm.Output);
    }

    [Fact]
    public async Task GenerateCommand_WithFailure_ClearsOutput()
    {
        var vm = Create(out var svc, out _);
        svc.Result = ToolResult<UuidReport>.Fail(ToolErrorKind.Validation, "x", "err");
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Empty(vm.Output);
    }

    [Fact]
    public void CopyOutput_WithValue_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.Output = "abc";
        vm.CopyOutputCommand.Execute(null);
        Assert.Equal("abc", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.Output = "x";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.Output);
    }
}