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

public sealed class MxLookupViewModelTests
{
    private sealed class FakeSvc : IMxLookupService
    {
        public TimeSpan Timeout => TimeSpan.FromSeconds(10);
        public ToolResult<MxLookupReport>? Result { get; set; }
        public Task<ToolResult<MxLookupReport>> LookupAsync(string? d, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<MxLookupReport>.Ok(new MxLookupReport(
                d ?? "", new List<MxRecord> { new(10, "mail.example.com") }, TimeSpan.Zero)));
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
    private static MxLookupViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new MxLookupViewModel(svc, fe, new FakeLoc(), NullLogger<MxLookupViewModel>.Instance);
    }

    [Fact] public async Task Lookup_Success() { var vm = Create(out _, out _); vm.Domain = "example.com"; await vm.LookupCommand.ExecuteAsync(null); Assert.Single(vm.MxRecords); }
    [Fact] public async Task Lookup_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<MxLookupReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.Domain = "example.com"; await vm.LookupCommand.ExecuteAsync(null); Assert.Empty(vm.MxRecords); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.MxRecords.Add(new MxRow(10, "mx.example.com")); vm.CopyResultsCommand.Execute(null); Assert.NotNull(fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Domain = "x"; vm.MxRecords.Add(new MxRow(10, "y")); vm.ClearCommand.Execute(null); Assert.Empty(vm.Domain); Assert.Empty(vm.MxRecords); }
}