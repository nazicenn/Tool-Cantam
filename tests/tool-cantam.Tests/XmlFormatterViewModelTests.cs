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

public sealed class XmlFormatterViewModelTests
{
    private sealed class FakeSvc : IXmlFormatterService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<XmlReport>? FormatResult { get; set; }
        public ToolResult<XmlReport>? MinifyResult { get; set; }
        public ToolResult<XmlValidation>? ValidateResult { get; set; }
        public Task<ToolResult<XmlReport>> FormatAsync(string? i, XmlOptions o, CancellationToken c) => Task.FromResult(FormatResult ?? ToolResult<XmlReport>.Ok(new XmlReport("<x/>", 0, 4, TimeSpan.Zero)));
        public Task<ToolResult<XmlReport>> MinifyAsync(string? i, CancellationToken c) => Task.FromResult(MinifyResult ?? ToolResult<XmlReport>.Ok(new XmlReport("<x/>", 0, 4, TimeSpan.Zero)));
        public Task<ToolResult<XmlValidation>> ValidateAsync(string? i, CancellationToken c) => Task.FromResult(ValidateResult ?? ToolResult<XmlValidation>.Ok(new XmlValidation(true, null, 1, 0)));
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

    private static XmlFormatterViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new XmlFormatterViewModel(svc, fe, new FakeLoc(), NullLogger<XmlFormatterViewModel>.Instance);
    }

    [Fact] public void Limits_Contains() { var vm = Create(out _, out _); Assert.Contains("10", vm.LimitsText); }
    [Fact] public async Task Format_Success() { var vm = Create(out _, out _); vm.InputXml = "x"; await vm.FormatCommand.ExecuteAsync(null); Assert.Equal("<x/>", vm.OutputXml); }
    [Fact] public async Task Format_Fail() { var vm = Create(out var s, out _); s.FormatResult = ToolResult<XmlReport>.Fail(ToolErrorKind.Validation, "x", "e"); vm.InputXml = "x"; await vm.FormatCommand.ExecuteAsync(null); Assert.Empty(vm.OutputXml); }
    [Fact] public void Copy() { var vm = Create(out _, out var fe); vm.OutputXml = "<a/>"; vm.CopyOutputCommand.Execute(null); Assert.Equal("<a/>", fe.LastCopied); }
    [Fact] public void Clear() { var vm = Create(out _, out _); vm.InputXml = "x"; vm.OutputXml = "y"; vm.ClearCommand.Execute(null); Assert.Empty(vm.InputXml); Assert.Empty(vm.OutputXml); }
}