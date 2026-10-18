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

public sealed class GeoIpLookupViewModelTests
{
    private sealed class FakeSvc : IGeoIpLookupService
    {
        public TimeSpan RequestTimeout => TimeSpan.FromSeconds(10);
        public ToolResult<GeoIpReport>? LookupResult { get; set; }
        public ToolResult<string>? MyIpResult { get; set; }

        public Task<ToolResult<GeoIpReport>> LookupAsync(string? ip, CancellationToken ct)
            => Task.FromResult(LookupResult ?? ToolResult<GeoIpReport>.Ok(new GeoIpReport(
                ip ?? "", "Türkiye", "TR", "Istanbul", "Istanbul", "34000",
                41.0, 29.0, "Europe/Istanbul", "Turk Telekom", "TT", false)));

        public Task<ToolResult<string>> GetMyIpAsync(CancellationToken ct)
            => Task.FromResult(MyIpResult ?? ToolResult<string>.Ok("8.8.8.8"));
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
    private static GeoIpLookupViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new GeoIpLookupViewModel(svc, fe, new FakeLoc(), NullLogger<GeoIpLookupViewModel>.Instance);
    }

    [Fact] public async Task Lookup_Success() { var vm = Create(out _, out _); vm.IpAddress = "8.8.8.8"; await vm.LookupCommand.ExecuteAsync(null); Assert.Equal("Türkiye", vm.Country); Assert.Equal("Istanbul", vm.City); }
    [Fact] public async Task Lookup_Fail() { var vm = Create(out var s, out _); s.LookupResult = ToolResult<GeoIpReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.IpAddress = "8.8.8.8"; await vm.LookupCommand.ExecuteAsync(null); Assert.Empty(vm.Country); }
    [Fact] public async Task MyIp_Success() { var vm = Create(out _, out _); await vm.LookupMyIpCommand.ExecuteAsync(null); Assert.Equal("8.8.8.8", vm.IpAddress); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.Country = "TR"; vm.CopyResultsCommand.Execute(null); Assert.NotNull(fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.IpAddress = "x"; vm.Country = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.IpAddress); Assert.Empty(vm.Country); }
}