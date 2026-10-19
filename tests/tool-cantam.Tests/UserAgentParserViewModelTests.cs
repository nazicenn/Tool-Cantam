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

public sealed class UserAgentParserViewModelTests
{
    private sealed class FakeSvc : IUserAgentParserService
    {
        public int MaxInputLength => 4096;
        public ToolResult<UserAgentReport>? Result { get; set; }
        public Task<ToolResult<UserAgentReport>> ParseAsync(string? ua, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<UserAgentReport>.Ok(new UserAgentReport("Chrome", "120", "Windows 10/11", "10.0", "Desktop PC", "Microsoft", false, false, TimeSpan.Zero)));
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
    private static UserAgentParserViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new UserAgentParserViewModel(svc, fe, new FakeLoc(), NullLogger<UserAgentParserViewModel>.Instance);
    }

    [Fact] public async Task Parse_Success() { var vm = Create(out _, out _); await vm.ParseCommand.ExecuteAsync(null); Assert.Equal("Chrome", vm.Browser); }
    [Fact] public async Task Parse_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<UserAgentReport>.Fail(ToolErrorKind.Validation, "x", "e"); await vm.ParseCommand.ExecuteAsync(null); Assert.Empty(vm.Browser); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.UserAgent = "test"; vm.CopyUserAgentCommand.Execute(null); Assert.Equal("test", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.UserAgent = "x"; vm.Browser = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.UserAgent); Assert.Empty(vm.Browser); }
}