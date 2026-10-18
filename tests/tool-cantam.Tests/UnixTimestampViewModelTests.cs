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

public sealed class UnixTimestampViewModelTests
{
    private sealed class FakeUnixService : IUnixTimestampService
    {
        public long MinSeconds => -62135596800L;
        public long MaxSeconds => 253402300799L;

        public ToolResult<UnixTimestampReport>? FromResult { get; set; }
        public ToolResult<UnixTimestampReport>? ToResult { get; set; }
        public UnixTimestampReport NowValue { get; set; } =
            new(1733673600L, 1733673600000L,
                new DateTime(2024, 12, 8, 20, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 12, 8, 23, 0, 0, DateTimeKind.Local),
                "2024-12-08T20:00:00Z", "Sun, 08 Dec 2024 20:00:00 GMT",
                "2024-12-08 20:00:00 UTC", TimeDisplayMode.Utc);

        public Task<ToolResult<UnixTimestampReport>> FromUnixAsync(string? input, TimestampUnit u, TimeDisplayMode m, CancellationToken ct)
            => Task.FromResult(FromResult ?? ToolResult<UnixTimestampReport>.Ok(NowValue));

        public Task<ToolResult<UnixTimestampReport>> ToUnixAsync(string? input, TimeDisplayMode m, CancellationToken ct)
            => Task.FromResult(ToResult ?? ToolResult<UnixTimestampReport>.Ok(NowValue));

        public UnixTimestampReport Now() => NowValue;
    }

    private sealed class FakeFileExportService : IFileExportService
    {
        public string? OpenPathResult { get; set; }
        public string? SavePathResult { get; set; }
        public string? LastCopied { get; private set; }

        public string? RequestOpenPath(string filter, string title) => OpenPathResult;
        public string? RequestSavePath(string filter, string defaultExt, string fileName) => SavePathResult;
        public Task<bool> ExportTextAsync(string text, string path, string filter, string title) => Task.FromResult(true);
        public void CopyToClipboard(string text) => LastCopied = text;
    }

    private sealed class FakeLocalizer : ILocalizerService
    {
        private string _culture = "tr-TR";
        public string CurrentCulture => _culture;
        public IReadOnlyCollection<string> AvailableCultures => new[] { "tr-TR", "en-US" };
        public string Get(string key) => key;
        public string Format(string key, params object?[] args)
            => args is { Length: > 0 } ? $"{key} [{string.Join(", ", args)}]" : key;
        public bool SetCulture(string c) { _culture = c; return true; }
    }

    private static UnixTimestampViewModel Create(
        out FakeUnixService svc, out FakeFileExportService fe)
    {
        svc = new FakeUnixService();
        fe = new FakeFileExportService();
        return new UnixTimestampViewModel(svc, fe, new FakeLocalizer(), NullLogger<UnixTimestampViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsRange()
    {
        var vm = Create(out _, out _);
        Assert.Contains("-62135596800", vm.LimitsText);
    }

    [Fact]
    public void Now_IsSetOnConstruction()
    {
        var vm = Create(out _, out _);
        Assert.Equal("1733673600", vm.CurrentTimestamp);
    }

    [Fact]
    public async Task TimestampToDate_WithSuccess_PopulatesOutputs()
    {
        var vm = Create(out _, out _);
        vm.TimestampInput = "1733673600";
        await vm.TimestampToDateCommand.ExecuteAsync(null);
        Assert.Equal("2024-12-08 20:00:00 UTC", vm.ReadableOutput);
        Assert.Equal("2024-12-08T20:00:00Z", vm.IsoOutput);
        Assert.Equal("1733673600", vm.UnixSecondsOutput);
    }

    [Fact]
    public async Task TimestampToDate_WithFailure_ClearsOutputs()
    {
        var vm = Create(out var svc, out _);
        svc.FromResult = ToolResult<UnixTimestampReport>.Fail(ToolErrorKind.Validation, "L.UnixTimestamp.Error.InvalidNumber", "x");
        vm.TimestampInput = "abc";
        await vm.TimestampToDateCommand.ExecuteAsync(null);
        Assert.Empty(vm.ReadableOutput);
        Assert.Empty(vm.IsoOutput);
    }

    [Fact]
    public async Task DateToTimestamp_WithSuccess_PopulatesOutputs()
    {
        var vm = Create(out _, out _);
        vm.DateInput = "2024-12-08 20:00:00";
        await vm.DateToTimestampCommand.ExecuteAsync(null);
        Assert.Equal("1733673600", vm.UnixSecondsOutput);
    }

    [Fact]
    public void UseNow_SetsInputs()
    {
        var vm = Create(out _, out _);
        vm.UseNowCommand.Execute(null);
        Assert.Equal("1733673600", vm.TimestampInput);
        Assert.False(string.IsNullOrWhiteSpace(vm.DateInput));
    }

    [Fact]
    public void CopyReadable_WithValue_CopiesToClipboard()
    {
        var vm = Create(out _, out var fe);
        vm.ReadableOutput = "hello";
        vm.CopyReadableCommand.Execute(null);
        Assert.Equal("hello", fe.LastCopied);
    }

    [Fact]
    public void Clear_ResetsAll()
    {
        var vm = Create(out _, out _);
        vm.TimestampInput = "x";
        vm.ReadableOutput = "y";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.TimestampInput);
        Assert.Empty(vm.ReadableOutput);
        Assert.Equal("1733673600", vm.CurrentTimestamp);
    }
}