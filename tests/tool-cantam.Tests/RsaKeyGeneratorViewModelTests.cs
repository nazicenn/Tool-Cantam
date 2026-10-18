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

public sealed class RsaKeyGeneratorViewModelTests
{
    private sealed class FakeSvc : IRsaKeyGeneratorService
    {
        public int[] AllowedKeySizes => new[] { 2048, 3072, 4096 };
        public ToolResult<RsaReport>? Result { get; set; }
        public Task<ToolResult<RsaReport>> GenerateAsync(RsaOptions o, CancellationToken c)
            => Task.FromResult(Result ?? ToolResult<RsaReport>.Ok(new RsaReport("PUB", "PRIV", 2048, RsaFormat.Pem, TimeSpan.Zero)));
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
    private static RsaKeyGeneratorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new RsaKeyGeneratorViewModel(svc, fe, new FakeLoc(), NullLogger<RsaKeyGeneratorViewModel>.Instance);
    }

    [Fact] public async Task Generate_Success() { var vm = Create(out _, out _); await vm.GenerateCommand.ExecuteAsync(null); Assert.Equal("PUB", vm.PublicKey); Assert.Equal("PRIV", vm.PrivateKey); }
    [Fact] public async Task Generate_Fail() { var vm = Create(out var s, out _); s.Result = ToolResult<RsaReport>.Fail(ToolErrorKind.Validation, "x", "e"); await vm.GenerateCommand.ExecuteAsync(null); Assert.Empty(vm.PublicKey); }
    [Fact] public void CopyPublic() { var vm = Create(out _, out var fe); vm.PublicKey = "PUB"; vm.CopyPublicCommand.Execute(null); Assert.Equal("PUB", fe.LastCopied); }
    [Fact] public void CopyPrivate() { var vm = Create(out _, out var fe); vm.PrivateKey = "PRIV"; vm.CopyPrivateCommand.Execute(null); Assert.Equal("PRIV", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.PublicKey = "P"; vm.PrivateKey = "Q"; vm.ClearCommand.Execute(null); Assert.Empty(vm.PublicKey); Assert.Empty(vm.PrivateKey); }
}