using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using tool_cantam.ViewModels;
using Xunit;

namespace tool_cantam.Tests;

public sealed class SqlFormatterViewModelTests
{
    private sealed class FakeSvc : ISqlFormatterService
    {
        public int MaxInputLength => 10 * 1024 * 1024;
        public ToolResult<SqlReport>? FormatResult { get; set; }
        public ToolResult<SqlReport>? MinifyResult { get; set; }
        public ToolResult<SqlValidation>? ValidateResult { get; set; }

        public Task<ToolResult<SqlReport>> FormatAsync(string? i, SqlOptions o, CancellationToken c)
            => Task.FromResult(FormatResult ?? ToolResult<SqlReport>.Ok(new SqlReport("SELECT 1", 0, 8, TimeSpan.Zero)));
        public Task<ToolResult<SqlReport>> MinifyAsync(string? i, CancellationToken c)
            => Task.FromResult(MinifyResult ?? ToolResult<SqlReport>.Ok(new SqlReport("x", 0, 1, TimeSpan.Zero)));
        public Task<ToolResult<SqlValidation>> ValidateAsync(string? i, CancellationToken c)
            => Task.FromResult(ValidateResult ?? ToolResult<SqlValidation>.Ok(new SqlValidation(true, null, 1)));
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

    private static SqlFormatterViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc(); fe = new FakeFe();
        return new SqlFormatterViewModel(svc, fe, new FakeLoc(), NullLogger<SqlFormatterViewModel>.Instance);
    }

    [Fact]
    public void LimitsText_ContainsLimit() { var vm = Create(out _, out _); Assert.Contains("10", vm.LimitsText); }

    [Fact]
    public async Task FormatCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        vm.InputSql = "select 1";
        await vm.FormatCommand.ExecuteAsync(null);
        Assert.Equal("SELECT 1", vm.OutputSql);
    }

    [Fact]
    public async Task FormatCommand_WithFailure_ClearsOutput()
    {
        var vm = Create(out var svc, out _);
        svc.FormatResult = ToolResult<SqlReport>.Fail(ToolErrorKind.Validation, "x", "err");
        vm.InputSql = "x";
        await vm.FormatCommand.ExecuteAsync(null);
        Assert.Empty(vm.OutputSql);
    }

    [Fact]
    public async Task MinifyCommand_WithSuccess_SetsOutput()
    {
        var vm = Create(out _, out _);
        vm.InputSql = "x";
        await vm.MinifyCommand.ExecuteAsync(null);
        Assert.Equal("x", vm.OutputSql);
    }

    [Fact]
    public async Task ValidateCommand_Valid_Success()
    {
        var vm = Create(out _, out _);
        vm.InputSql = "SELECT 1";
        await vm.ValidateCommand.ExecuteAsync(null);
        Assert.Equal(NotificationType.Success, vm.StatusType);
    }

    [Fact]
    public void CopyOutput_Copies()
    {
        var vm = Create(out _, out var fe);
        vm.OutputSql = "SELECT 1";
        vm.CopyOutputCommand.Execute(null);
        Assert.Equal("SELECT 1", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.InputSql = "x"; vm.OutputSql = "y";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.InputSql); Assert.Empty(vm.OutputSql);
    }
}