using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class FindReplaceServiceTests
{
    private readonly FindReplaceService _service = new();

    private static FindReplaceOptions Opts(
        bool matchCase = false, bool wholeWord = false, bool regex = false)
        => new(matchCase, wholeWord, regex);

    [Fact]
    public async Task ReplaceAsync_Simple_ReplacesAll()
    {
        var r = await _service.ReplaceAsync("hello hello", "hello", "hi", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("hi hi", r.Value!.Output);
        Assert.Equal(2, r.Value.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceAsync_MatchCase_RespectsCase()
    {
        var r = await _service.ReplaceAsync("Hello hello", "hello", "hi", Opts(matchCase: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("Hello hi", r.Value!.Output);
        Assert.Equal(1, r.Value.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceAsync_IgnoreCase_ReplacesBoth()
    {
        var r = await _service.ReplaceAsync("Hello hello", "hello", "hi", Opts(matchCase: false), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("hi hi", r.Value!.Output);
        Assert.Equal(2, r.Value.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceAsync_WholeWord_RespectsBoundaries()
    {
        var r = await _service.ReplaceAsync("cat concatenate cat", "cat", "dog",
            Opts(wholeWord: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("dog concatenate dog", r.Value!.Output);
        Assert.Equal(2, r.Value.ReplaceCount);
    }

    [Fact]
    public async Task ReplaceAsync_WholeWord_TurkishChars_Works()
    {
        var r = await _service.ReplaceAsync("çay çaydanlık çay", "çay", "kahve",
            Opts(wholeWord: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("kahve çaydanlık kahve", r.Value!.Output);
    }

    [Fact]
    public async Task ReplaceAsync_Regex_Works()
    {
        var r = await _service.ReplaceAsync("abc123def456", @"\d+", "X",
            Opts(regex: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("abcXdefX", r.Value!.Output);
    }

    [Fact]
    public async Task ReplaceAsync_InvalidRegex_ReturnsInvalidFormat()
    {
        var r = await _service.ReplaceAsync("abc", "[", "x", Opts(regex: true), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task ReplaceAsync_EmptyInput_ReturnsValidation()
    {
        var r = await _service.ReplaceAsync("", "x", "y", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ReplaceAsync_EmptyFind_ReturnsValidation()
    {
        var r = await _service.ReplaceAsync("abc", "", "y", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ReplaceAsync_NoMatches_ReturnsZero()
    {
        var r = await _service.ReplaceAsync("abc", "xyz", "y", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.ReplaceCount);
        Assert.Equal("abc", r.Value.Output);
    }
}