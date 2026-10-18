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

public sealed class LoremIpsumViewModelTests
{
    private sealed class FakeSvc : ILoremIpsumService
    {
        public int MinParagraphs => 1;
        public int MaxParagraphs => 50;
        public int MinSentences => 1;
        public int MaxSentences => 20;
        public ToolResult<LoremReport>? Result { get; set; }
        public Task<ToolResult<LoremReport>> GenerateAsync(LoremOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<LoremReport>.Ok(new LoremReport("Lorem ipsum test.", 1, 3, TimeSpan.Zero)));
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
    private static LoremIpsumViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new LoremIpsumViewModel(svc, fe, new FakeLoc(), NullLogger<LoremIpsumViewModel>.Instance);
    }

    [Fact] public async Task Generate_Success() { var vm = Create(out _, out _); await vm.GenerateCommand.ExecuteAsync(null); Assert.Equal("Lorem ipsum test.", vm.OutputText); }
    [Fact] public async Task Generate_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<LoremReport>.Fail(ToolErrorKind.Validation, "x", "e"); await vm.GenerateCommand.ExecuteAsync(null); Assert.Empty(vm.OutputText); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputText = "x"; vm.CopyOutputCommand.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.OutputText = "x"; vm.ClearCommand.Execute(null); Assert.Empty(vm.OutputText); }
}