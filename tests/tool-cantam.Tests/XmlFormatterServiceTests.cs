using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class XmlFormatterServiceTests
{
    private readonly XmlFormatterService _service = new();
    private static XmlOptions Opts(int i = 2, bool omit = false) => new(i, omit);

    [Fact]
    public async Task FormatAsync_Valid_Formats()
    {
        var r = await _service.FormatAsync("<root><a>1</a><b>2</b></root>", Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("\n", r.Value!.Output);
    }

    [Fact]
    public async Task FormatAsync_Invalid_ReturnsInvalidFormat()
    {
        var r = await _service.FormatAsync("<root><a></root>", Opts(), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task MinifyAsync_RemovesWhitespace()
    {
        var r = await _service.MinifyAsync("<root>\n  <a>1</a>\n</root>", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.DoesNotContain("\n", r.Value!.Output);
    }

    [Fact]
    public async Task ValidateAsync_Valid_True()
    {
        var r = await _service.ValidateAsync("<root><a>1</a></root>", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.IsValid);
        Assert.Equal(2, r.Value.ElementCount);
    }

    [Fact]
    public async Task ValidateAsync_Invalid_False()
    {
        var r = await _service.ValidateAsync("<root>", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.False(r.Value!.IsValid);
    }
}