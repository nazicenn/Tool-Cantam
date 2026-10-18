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

public sealed class BcryptGeneratorViewModelTests
{
    private sealed class FakeSvc : IBcryptGeneratorService
    {
        public int MinWorkFactor => 4;
        public int MaxWorkFactor => 14;
        public ToolResult<BcryptHashReport>? HashResult { get; set; }
        public ToolResult<BcryptVerifyReport>? VerifyResult { get; set; }
        public Task<ToolResult<BcryptHashReport>> HashAsync(string? p, int w, CancellationToken c) => Task.FromResult(HashResult ?? ToolResult<BcryptHashReport>.Ok(new BcryptHashReport("$2a$hash", w, TimeSpan.Zero)));
        public Task<ToolResult<BcryptVerifyReport>> VerifyAsync(string? p, string? h, CancellationToken c) => Task.FromResult(VerifyResult ?? ToolResult<BcryptVerifyReport>.Ok(new BcryptVerifyReport(true, TimeSpan.Zero)));
        public string GeneratePassword(int length = 16) => new string('x', length);
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
    private static BcryptGeneratorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new BcryptGeneratorViewModel(svc, fe, new FakeLoc(), NullLogger<BcryptGeneratorViewModel>.Instance);
    }

    [Fact] public void Limits() { var vm = Create(out _, out _); Assert.Contains("4", vm.LimitsText); }
    [Fact] public async Task Hash_Success() { var vm = Create(out _, out _); vm.Password = "x"; await vm.HashCommand.ExecuteAsync(null); Assert.Equal("$2a$hash", vm.HashedPassword); }
    [Fact] public async Task Verify_Match() { var vm = Create(out _, out _); vm.VerifyPassword = "x"; vm.HashedPassword = "$2a$hash"; await vm.VerifyCommand.ExecuteAsync(null); Assert.True(vm.IsVerified); }
    [Fact] public void RandomPassword() { var vm = Create(out _, out _); vm.GenerateRandomPasswordCommand.Execute(null); Assert.Equal(16, vm.Password.Length); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.HashedPassword = "x"; vm.CopyHashCommand.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.Password = "x"; vm.HashedPassword = "y"; vm.VerifyPassword = "z"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Password); Assert.Empty(vm.HashedPassword); Assert.Empty(vm.VerifyPassword); }
}