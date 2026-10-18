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

public sealed class CronParserViewModelTests
{
    private sealed class FakeSvc : ICronParserService
    {
        public ToolResult<CronReport>? Result { get; set; }
        public Task<ToolResult<CronReport>> ParseAsync(string? e, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<CronReport>.Ok(new CronReport(e ?? "", new List<CronField>
            {
                new("Minute", "0", "at minute 0", 0, 59),
                new("Hour", "0", "at 0:00", 0, 23),
                new("DayOfMonth", "*", "every day", 1, 31),
                new("Month", "*", "every month", 1, 12),
                new("DayOfWeek", "*", "every day", 0, 6),
            }, "Midnight", new List<DateTime> { DateTime.UtcNow }, TimeSpan.Zero)));
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
    private static CronParserViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new CronParserViewModel(svc, fe, new FakeLoc(), NullLogger<CronParserViewModel>.Instance);
    }

    [Fact] public async Task Parse_Success() { var vm = Create(out _, out _); vm.CronExpression = "0 0 * * *"; await vm.ParseCommand.ExecuteAsync(null); Assert.Equal(5, vm.Fields.Count); }
    [Fact] public async Task Parse_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<CronReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.CronExpression = "bad"; await vm.ParseCommand.ExecuteAsync(null); Assert.Empty(vm.Fields); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.Result = "x"; vm.CopyResultCommand.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.CronExpression = "x"; vm.Result = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.CronExpression); Assert.Empty(vm.Result); }
}