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

public sealed class CurlGeneratorViewModelTests
{
    private sealed class FakeSvc : ICurlGeneratorService
    {
        public ToolResult<CurlReport>? Result { get; set; }
        public Task<ToolResult<CurlReport>> GenerateAsync(CurlOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<CurlReport>.Ok(new CurlReport("curl -X GET \"x\"", 2, TimeSpan.Zero)));
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
    private static CurlGeneratorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new CurlGeneratorViewModel(svc, fe, new FakeLoc(), NullLogger<CurlGeneratorViewModel>.Instance);
    }

    [Fact] public async Task Generate_Success() { var vm = Create(out _, out _); vm.Url = "https://x"; await vm.GenerateCommand.ExecuteAsync(null); Assert.Contains("curl", vm.CurlCommand); }
    [Fact] public async Task Generate_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<CurlReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.Url = "https://x"; await vm.GenerateCommand.ExecuteAsync(null); Assert.Empty(vm.CurlCommand); }
    [Fact] public void MethodPost_ShowsBody() { var vm = Create(out _, out _); vm.Method = "POST"; Assert.True(vm.IsBodyVisible); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.CurlCommand = "curl x"; vm.CopyCurlCommand.Execute(null); Assert.Equal("curl x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Url = "x"; vm.CurlCommand = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Url); Assert.Empty(vm.CurlCommand); }
}