using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class JsonMinifierServiceTests
{
    private readonly JsonMinifierService _service = new();

    [Fact]
    public async Task MinifyAsync_ValidJson_RemovesWhitespace()
    {
        var r = await _service.MinifyAsync("{ \"a\" : 1 }", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal("{\"a\":1}", r.Value!.Output);
    }

    [Fact]
    public async Task FormatAsync_ValidJson_Indents()
    {
        var r = await _service.FormatAsync("{\"a\":1}", 2, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("\n", r.Value!.Output);
    }

    [Fact]
    public async Task MinifyAsync_Empty_ReturnsValidation()
    {
        var r = await _service.MinifyAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task MinifyAsync_InvalidJson_ReturnsInvalidFormat()
    {
        var r = await _service.MinifyAsync("{invalid}", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task MinifyAsync_CountsNodes()
    {
        var r = await _service.MinifyAsync("{\"a\":1,\"b\":{\"c\":2}}", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(4, r.Value!.NodeCount);
    }
}