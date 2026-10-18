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

public sealed class AesEncryptViewModelTests
{
    private sealed class FakeSvc : IAesEncryptService
    {
        public int MinPasswordLength => 8;
        public ToolResult<AesReport>? EncryptResult { get; set; }
        public ToolResult<AesReport>? DecryptResult { get; set; }
        public Task<ToolResult<AesReport>> EncryptAsync(string? p, string? pw, CancellationToken c) => Task.FromResult(EncryptResult ?? ToolResult<AesReport>.Ok(new AesReport("ENC", 0, 3, TimeSpan.Zero)));
        public Task<ToolResult<AesReport>> DecryptAsync(string? c2, string? pw, CancellationToken c) => Task.FromResult(DecryptResult ?? ToolResult<AesReport>.Ok(new AesReport("PLAIN", 0, 5, TimeSpan.Zero)));
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
    private static AesEncryptViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new AesEncryptViewModel(svc, fe, new FakeLoc(), NullLogger<AesEncryptViewModel>.Instance);
    }

    [Fact] public void Limits() { var vm = Create(out _, out _); Assert.Contains("8", vm.LimitsText); }
    [Fact] public async Task Encrypt_Success() { var vm = Create(out _, out _); vm.InputText = "x"; vm.Password = "password123"; await vm.EncryptCommand.ExecuteAsync(null); Assert.Equal("ENC", vm.OutputText); }
    [Fact] public async Task Encrypt_Fail() { var vm = Create(out var s, out _); s.EncryptResult = ToolResult<AesReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.InputText = "x"; vm.Password = "password123"; await vm.EncryptCommand.ExecuteAsync(null); Assert.Empty(vm.OutputText); }
    [Fact] public async Task Decrypt_Success() { var vm = Create(out _, out _); vm.InputText = "x"; vm.Password = "password123"; await vm.DecryptCommand.ExecuteAsync(null); Assert.Equal("PLAIN", vm.OutputText); }
    [Fact] public void GenPassword() { var vm = Create(out _, out _); vm.GeneratePasswordCommand.Execute(null); Assert.Equal(16, vm.Password.Length); }
    [Fact] public void Swap() { var vm = Create(out _, out _); vm.InputText = "A"; vm.OutputText = "B"; vm.SwapCommand.Execute(null); Assert.Equal("B", vm.InputText); Assert.Equal("A", vm.OutputText); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputText = "x"; vm.CopyOutputCommand.Execute(null); Assert.Equal("x", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputText = "x"; vm.OutputText = "y"; vm.Password = "p"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputText); Assert.Empty(vm.OutputText); Assert.Empty(vm.Password); }
}