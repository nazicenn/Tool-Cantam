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

public sealed class TracerouteViewModelTests
{
    private sealed class FakeSvc : ITracerouteService
    {
        public int MinHops => 1;
        public int MaxHopsLimit => 64;
        public ToolResult<TraceReport>? Result { get; set; }

        public Task<ToolResult<TraceReport>> TraceAsync(string? h, int m, IProgress<TraceHop>? p, CancellationToken ct)
        {
            if (Result is { IsSuccess: false })
                return Task.FromResult(Result);

            p?.Report(new TraceHop(1, "1.1.1.1", 10, "TtlExpired"));
            return Task.FromResult(Result ?? ToolResult<TraceReport>.Ok(new TraceReport(
                h ?? "", new List<TraceHop> { new(1, "1.1.1.1", 10, "TtlExpired") }, TimeSpan.Zero, false)));
        }
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

    private static TracerouteViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc();
        fe = new FakeFe();
        return new TracerouteViewModel(svc, fe, new FakeLoc(), NullLogger<TracerouteViewModel>.Instance);
    }

    [Fact]
    public async Task Trace_Success()
    {
        var vm = Create(out _, out _);
        vm.Host = "8.8.8.8";
        await vm.StartTraceCommand.ExecuteAsync(null);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.True(vm.Hops.Count >= 1);
    }

    [Fact]
    public async Task Trace_Fail()
    {
        var vm = Create(out var s, out _);
        s.Result = ToolResult<TraceReport>.Fail(ToolErrorKind.Validation, "x", "e");
        vm.Host = "x";
        await vm.StartTraceCommand.ExecuteAsync(null);
        Assert.Empty(vm.Hops);
    }

    [Fact]
    public void Copy()
    {
        var vm = Create(out _, out var fe);
        vm.Hops.Add(new TraceHopRow(1, "1.1.1.1", 10, "ok"));
        vm.CopyResultsCommand.Execute(null);
        Assert.NotNull(fe.LastCopied);
    }

    [Fact]
    public void Clear()
    {
        var vm = Create(out _, out _);
        vm.Host = "x";
        vm.Hops.Add(new TraceHopRow(1, "y", 0, "z"));
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.Host);
        Assert.Empty(vm.Hops);
    }
}