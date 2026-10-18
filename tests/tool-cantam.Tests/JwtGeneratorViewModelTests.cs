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

public sealed class JwtGeneratorViewModelTests
{
    private sealed class FakeSvc : IJwtGeneratorService
    {
        public int MinKeyLength => 32;
        public int MaxKeyLength => 4096;
        public ToolResult<JwtGenReport>? Result { get; set; }

        public Task<ToolResult<JwtGenReport>> GenerateAsync(JwtOptions o, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<JwtGenReport>.Ok(
                new JwtGenReport("a.b.c", "HS256", DateTime.UtcNow.AddHours(1), 1, 1, 1, 1, TimeSpan.Zero)));
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

    private static JwtGeneratorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc();
        fe = new FakeFe();
        return new JwtGeneratorViewModel(svc, fe, new FakeLoc(), NullLogger<JwtGeneratorViewModel>.Instance);
    }

    [Fact]
    public void Constructor_GeneratesKey()
    {
        var vm = Create(out _, out _);
        Assert.Equal(64, vm.SecretKey.Length);
    }

    [Fact]
    public async Task Generate_Success()
    {
        var vm = Create(out _, out _);
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Equal("a.b.c", vm.GeneratedToken);
    }

    [Fact]
    public async Task Generate_Fail()
    {
        var vm = Create(out var s, out _);
        s.Result = ToolResult<JwtGenReport>.Fail(ToolErrorKind.Validation, "x", "e");
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Empty(vm.GeneratedToken);
    }

    [Fact]
    public void Copy()
    {
        var vm = Create(out _, out var fe);
        vm.GeneratedToken = "x";
        vm.CopyTokenCommand.Execute(null);
        Assert.Equal("x", fe.LastCopied);
    }

    [Fact]
    public void Clear()
    {
        var vm = Create(out _, out _);
        vm.GeneratedToken = "x";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.GeneratedToken);
    }
}