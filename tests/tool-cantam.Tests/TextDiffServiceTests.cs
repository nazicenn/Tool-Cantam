using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class TextDiffServiceTests
{
    private readonly TextDiffService _service = new();

    private static DiffOptions Opts(
        bool ignoreCase = false, bool ignoreWs = false, bool ignoreEmpty = false)
        => new(ignoreCase, ignoreWs, ignoreEmpty);

    [Fact]
    public async Task CompareAsync_Identical_AllUnchanged()
    {
        var r = await _service.CompareAsync("a\nb\nc", "a\nb\nc", Opts(), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.Unchanged);
        Assert.Equal(0, r.Value.Added);
        Assert.Equal(0, r.Value.Removed);
        Assert.Equal(0, r.Value.Changed);
    }

    [Fact]
    public async Task CompareAsync_Added_DetectsAdditions()
    {
        var r = await _service.CompareAsync("a\nb", "a\nb\nc", Opts(), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value!.Added);
    }

    [Fact]
    public async Task CompareAsync_Removed_DetectsRemovals()
    {
        var r = await _service.CompareAsync("a\nb\nc", "a\nb", Opts(), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value!.Removed);
    }

    [Fact]
    public async Task CompareAsync_Changed_DetectsChange()
    {
        var r = await _service.CompareAsync("a\nb", "a\nB", Opts(), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value!.Changed);
    }

    [Fact]
    public async Task CompareAsync_IgnoreCase_NoChange()
    {
        var r = await _service.CompareAsync("a\nb", "A\nB", Opts(ignoreCase: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Changed);
        Assert.Equal(2, r.Value.Unchanged);
    }

    [Fact]
    public async Task CompareAsync_IgnoreWhitespace_NoChange()
    {
        var r = await _service.CompareAsync("a  b", "a b", Opts(ignoreWs: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Changed);
    }

    [Fact]
    public async Task CompareAsync_IgnoreEmpty_RemovesEmpty()
    {
        var r = await _service.CompareAsync("a\n\nb", "a\nb", Opts(ignoreEmpty: true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Added);
        Assert.Equal(0, r.Value.Removed);
    }

    [Fact]
    public async Task CompareAsync_BothEmpty_ReturnsValidation()
    {
        var r = await _service.CompareAsync("", "", Opts(), TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task CompareAsync_AddedThenChanged_DetectsBoth()
    {
        var r = await _service.CompareAsync("a\nb\nc", "a\nB\nc\nd", Opts(), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value!.Changed);
        Assert.Equal(1, r.Value.Added);
    }
}