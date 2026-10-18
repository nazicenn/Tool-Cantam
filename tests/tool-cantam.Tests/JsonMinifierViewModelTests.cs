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

public sealed class JsonMinifierViewModelTests
{
    private sealed class FakeSvc : IJsonMinifierService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<JsonMinReport>? Result { get; set; }
        public Task<ToolResult<JsonMinReport>> MinifyAsync(string? i, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<JsonMinReport>.Ok(new JsonMinReport("{\"a\":1}", 0, 7, 2, TimeSpan.Zero)));
        public Task<ToolResult<JsonMinReport>> FormatAsync(string? i, int s, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<JsonMinReport>.Ok(new JsonMinReport("{\n  \"a\": 1\n}", 0, 12, 2, TimeSpan.Zero)));
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
    private static JsonMinifierViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new JsonMinifierViewModel(svc, fe, new FakeLoc(), NullLogger<JsonMinifierViewModel>.Instance);
    }

    [Fact] public async Task Minify_Success() { var vm = Create(out _, out _); vm.InputJson = "x"; await vm.MinifyCommand.ExecuteAsync(null); Assert.Equal("{\"a\":1}", vm.OutputJson); }
    [Fact] public async Task Minify_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<JsonMinReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.InputJson = "x"; await vm.MinifyCommand.ExecuteAsync(null); Assert.Empty(vm.OutputJson); }
    [Fact] public async Task Format_Success() { var vm = Create(out _, out _); vm.InputJson = "x"; await vm.FormatCommand.ExecuteAsync(null); Assert.Contains("\n", vm.OutputJson); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputJson = "x"; vm.CopyOutputCommand.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputJson = "x"; vm.OutputJson = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputJson); Assert.Empty(vm.OutputJson); }
}