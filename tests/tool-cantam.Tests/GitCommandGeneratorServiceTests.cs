using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class GitCommandGeneratorServiceTests
{
    private readonly GitCommandGeneratorService _service = new();

    private static GitCommandOptions Opts(
        GitCommandCategory cat,
        string arg1 = "",
        string arg2 = "",
        bool f1 = false,
        bool f2 = false)
        => new(cat, arg1, arg2, f1, f2);

    [Fact]
    public async Task GenerateAsync_Commit_ReturnsCommand()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Commit, "test", f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("git commit", r.Value!.Command);
        Assert.Contains("-a", r.Value.Command);
        Assert.Contains("test", r.Value.Command);
    }

    [Fact]
    public async Task GenerateAsync_Branch_Create()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Branch, "feature"), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("git branch feature", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Branch_Delete()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Branch, "old", f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("-d", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Merge_NoFF()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Merge, "main", f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("--no-ff", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Log_Oneline()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Log, "5", f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("--oneline", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Stash_Pop()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Stash, f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("git stash pop", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Remote_List()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Remote, f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("git remote -v", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_Tag_Delete()
    {
        var r = await _service.GenerateAsync(Opts(GitCommandCategory.Tag, "v1.0", f1: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Contains("git tag -d v1.0", r.Value!.Command);
    }

    [Fact]
    public async Task GenerateAsync_NullOptions_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(null!, TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Categories_NotEmpty()
    {
        Assert.NotEmpty(_service.Categories);
    }
}