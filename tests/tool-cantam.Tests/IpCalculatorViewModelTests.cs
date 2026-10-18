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

public sealed class IpCalculatorViewModelTests
{
    private sealed class FakeSvc : IIpCalculatorService
    {
        public ToolResult<IpCalcReport>? Result { get; set; }
        public Task<ToolResult<IpCalcReport>> CalculateAsync(string? ip, int cidr, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<IpCalcReport>.Ok(new IpCalcReport(
                ip ?? "", cidr, "192.168.1.0", "192.168.1.255", "255.255.255.0", "0.0.0.255",
                "192.168.1.1", "192.168.1.254", 256, 254, "x", "y", true)));
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
    private static IpCalculatorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new IpCalculatorViewModel(svc, fe, new FakeLoc(), NullLogger<IpCalculatorViewModel>.Instance);
    }

    [Fact] public async Task Calculate_Success() { var vm = Create(out _, out _); vm.IpAddress = "192.168.1.100"; await vm.CalculateCommand.ExecuteAsync(null); Assert.Equal("192.168.1.0", vm.NetworkAddress); Assert.Equal(254, vm.UsableHosts); }
    [Fact] public async Task Calculate_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<IpCalcReport>.Fail(ToolErrorKind.Validation, "x", "e"); await vm.CalculateCommand.ExecuteAsync(null); Assert.Empty(vm.NetworkAddress); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.NetworkAddress = "x"; vm.CopyResultCommand.Execute(null); Assert.NotNull(fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.NetworkAddress = "x"; vm.ClearCommand.Execute(null); Assert.Empty(vm.NetworkAddress); }
}