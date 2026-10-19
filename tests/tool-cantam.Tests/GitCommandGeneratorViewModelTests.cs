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

public sealed class GitCommandGeneratorViewModelTests
{
    private sealed class FakeSvc : IGitCommandGeneratorService
    {
        public int MaxInputLength => 1024;
        public IReadOnlyList<GitCommandCategory> Categories => new[] { GitCommandCategory.Commit };
        public ToolResult<GitCommandReport>? Result { get; set; }
        public Task<ToolResult<GitCommandReport>> GenerateAsync(GitCommandOptions o, CancellationToken ct)
            => Task.FromResult(Result ?? ToolResult<GitCommandReport>.Ok(
                new GitCommandReport("git commit -m \"test\"", "Commit", "Commit", TimeSpan.Zero)));
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

    private static GitCommandGeneratorViewModel Create(out FakeSvc svc, out FakeFe fe)
    {
        svc = new FakeSvc();
        fe = new FakeFe();
        return new GitCommandGeneratorViewModel(svc, fe, new FakeLoc(),
            NullLogger<GitCommandGeneratorViewModel>.Instance);
    }

    [Fact]
    public void Constructor_PopulatesCategories()
    {
        var vm = Create(out _, out _);
        Assert.NotEmpty(vm.Categories);
    }

    [Fact]
    public async Task Generate_Success()
    {
        var vm = Create(out _, out _);
        vm.Argument1 = "test";
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Contains("git commit", vm.Command);
    }

    [Fact]
    public async Task Generate_Fail_Clears()
    {
        var vm = Create(out var s, out _);
        s.Result = ToolResult<GitCommandReport>.Fail(ToolErrorKind.Validation, "x", "e");
        await vm.GenerateCommand.ExecuteAsync(null);
        Assert.Empty(vm.Command);
    }

    [Fact]
    public void Copy_Works()
    {
        var vm = Create(out _, out var fe);
        vm.Command = "git status";
        vm.CopyCommand.Execute(null);
        Assert.Equal("git status", fe.LastCopied);
    }

    [Fact]
    public void Clear_Resets()
    {
        var vm = Create(out _, out _);
        vm.Command = "x";
        vm.Argument1 = "y";
        vm.ClearCommand.Execute(null);
        Assert.Empty(vm.Command);
        Assert.Empty(vm.Argument1);
    }
}