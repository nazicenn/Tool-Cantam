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

public sealed class RestApiClientViewModelTests
{
    private sealed class FakeSvc : IRestApiService
    {
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(30);
        public ToolResult<RestApiReport>? Result { get; set; }
        public Task<ToolResult<RestApiReport>> SendAsync(RestApiOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<RestApiReport>.Ok(new RestApiReport(200, "OK", "{}", new List<KeyValuePair<string, string>> { new("X", "1") }, TimeSpan.FromMilliseconds(50), 2)));
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
    private static RestApiClientViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new RestApiClientViewModel(svc, fe, new FakeLoc(), NullLogger<RestApiClientViewModel>.Instance);
    }

    [Fact] public async Task Send_Success() { var vm = Create(out _, out _); vm.Url = "https://x"; await vm.SendCommand.ExecuteAsync(null); Assert.Equal(200, vm.StatusCode); Assert.Single(vm.ResponseHeaders); }
    [Fact] public async Task Send_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<RestApiReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.Url = "https://x"; await vm.SendCommand.ExecuteAsync(null); Assert.Equal(0, vm.StatusCode); }
    [Fact] public void MethodPost_ShowsBody() { var vm = Create(out _, out _); vm.Method = "POST"; Assert.True(vm.IsBodyVisible); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.ResponseBody = "{}"; vm.StatusCode = 200; vm.ClearCommand.Execute(null); Assert.Empty(vm.ResponseBody); Assert.Equal(0, vm.StatusCode); }
}