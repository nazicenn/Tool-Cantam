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

public sealed class MimeTypeViewModelTests
{
    private sealed class FakeSvc : IMimeTypeService
    {
        public int MaxInputLength => 256;
        public IReadOnlyList<string> AllExtensions => new[] { "txt", "png" };
        public IReadOnlyList<string> AllMimeTypes => new[] { "text/plain", "image/png" };
        public ToolResult<MimeReport>? Result { get; set; }
        public Task<ToolResult<MimeReport>> LookupByExtensionAsync(string? e, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<MimeReport>.Ok(new MimeReport(e ?? "", "text/plain", "Plain text", "Text", true, false, false, false, false, false, TimeSpan.Zero)));
        public Task<ToolResult<MimeReport>> LookupByMimeAsync(string? m, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<MimeReport>.Ok(new MimeReport(m ?? "", "text/plain", "Plain text", "Text", true, false, false, false, false, false, TimeSpan.Zero)));
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
    private static MimeTypeViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new MimeTypeViewModel(svc, fe, new FakeLoc(), NullLogger<MimeTypeViewModel>.Instance);
    }

    [Fact] public void Constructor_PopulatesLists() { var vm = Create(out _, out _); Assert.NotEmpty(vm.AllExtensions); Assert.NotEmpty(vm.AllMimeTypes); }
    [Fact] public async Task Lookup_Extension_Success() { var vm = Create(out _, out _); vm.Input = "txt"; await vm.LookupCommand.ExecuteAsync(null); Assert.Equal("text/plain", vm.MimeType); }
    [Fact] public async Task Lookup_Mime_Success() { var vm = Create(out _, out _); vm.Input = "text/plain"; await vm.LookupCommand.ExecuteAsync(null); Assert.Equal("text/plain", vm.MimeType); }
    [Fact] public async Task Lookup_Fail_Clears() { var vm = Create(out var s, out _); s.Result = ToolResult<MimeReport>.Fail(ToolErrorKind.NotFound, "x", "e"); vm.Input = "txt"; await vm.LookupCommand.ExecuteAsync(null); Assert.Empty(vm.MimeType); }
    [Fact] public void CopyMime_Works() { var vm = Create(out _, out var fe); vm.MimeType = "text/plain"; vm.CopyMimeCommand.Execute(null); Assert.Equal("text/plain", fe.LastCopied); }
    [Fact] public void Clear_Resets() { var vm = Create(out _, out _); vm.Input = "x"; vm.MimeType = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.Input); Assert.Empty(vm.MimeType); }
}