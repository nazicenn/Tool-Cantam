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

public sealed class CsvToJsonViewModelTests
{
    private sealed class FakeSvc : ICsvToJsonService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<CsvReport>? Result { get; set; }
        public Task<ToolResult<CsvReport>> ConvertAsync(string? i, CsvOptions o, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<CsvReport>.Ok(new CsvReport("[{\"a\":1}]", 1, 1, TimeSpan.Zero)));
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
    private static CsvToJsonViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new CsvToJsonViewModel(svc, fe, new FakeLoc(), NullLogger<CsvToJsonViewModel>.Instance);
    }

    [Fact] public void Limits() { var vm = Create(out _, out _); Assert.Contains("10", vm.LimitsText); }
    [Fact] public async Task Convert_Success() { var vm = Create(out _, out _); vm.InputCsv = "a"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Contains("a", vm.OutputJson); }
    [Fact] public async Task Convert_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<CsvReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.InputCsv = "a"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Empty(vm.OutputJson); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputJson = "{}"; vm.CopyOutputCommand.Execute(null); Assert.Equal("{}", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputCsv = "a"; vm.OutputJson = "b"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputCsv); Assert.Empty(vm.OutputJson); }
}