using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class RegexTesterServiceTests
{
    private readonly RegexTesterService _service = new();

    private static RegexTestOptions Opts(
        bool ignoreCase = false, bool multiline = false, bool replaceMode = false, string? replaceWith = null)
        => new(ignoreCase, multiline, false, false, false, replaceMode, replaceWith);

    [Fact]
    public async Task TestAsync_SimplePattern_FindsMatches()
    {
        var r = await _service.TestAsync(@"\d+", "abc123def456", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value!.MatchCount);
    }

    [Fact]
    public async Task TestAsync_NoMatches_ReturnsZero()
    {
        var r = await _service.TestAsync(@"xyz", "abc", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.MatchCount);
    }

    [Fact]
    public async Task TestAsync_ReplaceMode_Replaces()
    {
        var r = await _service.TestAsync(@"\d+", "abc123", Opts(replaceMode: true, replaceWith: "X"), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("abcX", r.Value!.Replaced);
    }

    [Fact]
    public async Task TestAsync_InvalidPattern_ReturnsInvalidFormat()
    {
        var r = await _service.TestAsync("[", "abc", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task TestAsync_EmptyPattern_ReturnsValidation()
    {
        var r = await _service.TestAsync("", "abc", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task TestAsync_EmptyInput_ReturnsValidation()
    {
        var r = await _service.TestAsync(@"\d", "", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task TestAsync_HighlightsMatches()
    {
        var r = await _service.TestAsync(@"\d+", "a1b22", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("[1]", r.Value!.Highlighted);
        Assert.Contains("[22]", r.Value.Highlighted);
    }

    [Fact]
    public void GetSamples_ReturnsNonEmpty()
    {
        var samples = _service.GetSamples();
        Assert.NotEmpty(samples);
    }
}