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

public sealed class YamlToJsonViewModelTests
{
    private sealed class FakeSvc : IYamlToJsonService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<YamlToJsonReport>? Result { get; set; }
        public Task<ToolResult<YamlToJsonReport>> ConvertAsync(string? y, YamlToJsonOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<YamlToJsonReport>.Ok(new YamlToJsonReport("{\"a\":1}", 5, 7, TimeSpan.Zero)));
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
    private static YamlToJsonViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new YamlToJsonViewModel(svc, fe, new FakeLoc(), NullLogger<YamlToJsonViewModel>.Instance);
    }

    [Fact] public async Task Convert_Success() { var vm = Create(out _, out _); vm.InputYaml = "name: x"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Contains("a", vm.OutputJson); }
    [Fact] public async Task Convert_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<YamlToJsonReport>.Fail(ToolErrorKind.InvalidFormat, "x", "e"); vm.InputYaml = "x"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Empty(vm.OutputJson); }
    [Fact] public void Swap() { var vm = Create(out _, out _); vm.InputYaml = "a"; vm.OutputJson = "b"; vm.SwapCommand.Execute(null); Assert.Equal("b", vm.InputYaml); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputJson = "{}"; vm.CopyOutputCommand.Execute(null); Assert.Equal("{}", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputYaml = "x"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputYaml); }
}