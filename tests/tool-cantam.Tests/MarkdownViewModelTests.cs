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

public sealed class MarkdownViewModelTests
{
    private sealed class FakeSvc : IMarkdownService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<MarkdownReport>? Result { get; set; }
        public Task<ToolResult<MarkdownReport>> ToHtmlAsync(string? m, MarkdownOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<MarkdownReport>.Ok(new MarkdownReport("<h1>Test</h1>", 5, 15, 1, TimeSpan.Zero)));
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
    private static MarkdownViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new MarkdownViewModel(svc, fe, new FakeLoc(), NullLogger<MarkdownViewModel>.Instance);
    }

    [Fact] public async Task Convert_Success() { var vm = Create(out _, out _); vm.InputMarkdown = "# Test"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Contains("<h1", vm.OutputHtml); }
    [Fact] public async Task Convert_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<MarkdownReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.InputMarkdown = "x"; await vm.ConvertCommand.ExecuteAsync(null); Assert.Empty(vm.OutputHtml); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputHtml = "<p>x</p>"; vm.CopyHtmlCommand.Execute(null); Assert.Equal("<p>x</p>", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputMarkdown = "x"; vm.OutputHtml = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputMarkdown); Assert.Empty(vm.OutputHtml); }
}