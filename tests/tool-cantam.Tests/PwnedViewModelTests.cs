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

public sealed class PwnedViewModelTests
{
    private sealed class FakeSvc : IPwnedService
    {
        public ToolResult<PwnedReport>? Result { get; set; }
        public Task<ToolResult<PwnedReport>> CheckPasswordAsync(string? p, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<PwnedReport>.Ok(new PwnedReport(0, false, TimeSpan.Zero)));
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
    private static PwnedViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new PwnedViewModel(svc, fe, new FakeLoc(), NullLogger<PwnedViewModel>.Instance);
    }

    [Fact] public async Task Check_Safe() { var vm = Create(out _, out _); vm.Password = "x"; await vm.CheckCommand.ExecuteAsync(null); Assert.False(vm.IsPwned); }
    [Fact] public async Task Check_Pwned() { var vm = Create(out var s, out _); s.Result = ToolResult<PwnedReport>.Ok(new PwnedReport(100, true, TimeSpan.Zero)); vm.Password = "x"; await vm.CheckCommand.ExecuteAsync(null); Assert.True(vm.IsPwned); Assert.True(vm.ShowSuggestion); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Password = "x"; vm.ResultText = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Password); Assert.Empty(vm.ResultText); }
}