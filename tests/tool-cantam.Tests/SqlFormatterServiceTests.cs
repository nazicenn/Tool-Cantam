using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class SqlFormatterServiceTests
{
    private readonly SqlFormatterService _service = new();
    private static SqlOptions Opts(bool upper = true, int indent = 2, bool andOr = false, bool comma = false)
        => new(upper, indent, andOr, comma);

    [Fact]
    public async Task FormatAsync_Select_UppercasesKeywords()
    {
        var r = await _service.FormatAsync("select id from users", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("SELECT", r.Value!.Output);
        Assert.Contains("FROM", r.Value.Output);
    }

    [Fact]
    public async Task FormatAsync_Where_AddsNewline()
    {
        var r = await _service.FormatAsync("select * from users where id=1", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("\n", r.Value!.Output);
    }

    [Fact]
    public async Task FormatAsync_Empty_ReturnsValidation()
    {
        var r = await _service.FormatAsync("", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task MinifyAsync_RemovesWhitespace()
    {
        var r = await _service.MinifyAsync("SELECT   *\n  FROM   users", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("SELECT * FROM users", r.Value!.Output);
    }

    [Fact]
    public async Task ValidateAsync_Valid_ReturnsTrue()
    {
        var r = await _service.ValidateAsync("SELECT * FROM users WHERE id=1;", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_UnbalancedParen_ReturnsFalse()
    {
        var r = await _service.ValidateAsync("SELECT * FROM (users", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.False(r.Value!.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_UnclosedString_ReturnsFalse()
    {
        var r = await _service.ValidateAsync("SELECT * FROM users WHERE name='abc", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.False(r.Value!.IsValid);
    }
}