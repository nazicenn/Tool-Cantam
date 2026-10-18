using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class RgbColorServiceTests
{
    private readonly RgbColorService _service = new();

    [Fact]
    public async Task FromRgbAsync_Black_ReturnsCorrect()
    {
        var r = await _service.FromRgbAsync(0, 0, 0, TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal("#000000", r.Value!.Hex);
        Assert.Equal("rgb(0, 0, 0)", r.Value.Rgb);
    }

    [Fact]
    public async Task FromRgbAsync_White_ReturnsCorrect()
    {
        var r = await _service.FromRgbAsync(255, 255, 255, TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal("#FFFFFF", r.Value!.Hex);
    }

    [Fact]
    public async Task FromRgbAsync_Red_ReturnsCorrect()
    {
        var r = await _service.FromRgbAsync(255, 0, 0, TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal("#FF0000", r.Value!.Hex);
    }

    [Fact]
    public async Task FromRgbAsync_OutOfRange_ReturnsValidation()
    {
        var r = await _service.FromRgbAsync(300, 0, 0, TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task FromHexAsync_Valid_ReturnsRgb()
    {
        var r = await _service.FromHexAsync("#FF0000", TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(255, r.Value!.Red);
        Assert.Equal(0, r.Value.Green);
        Assert.Equal(0, r.Value.Blue);
    }

    [Fact]
    public async Task FromHexAsync_ShortHex_Expands()
    {
        var r = await _service.FromHexAsync("#F00", TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(255, r.Value!.Red);
    }

    [Fact]
    public async Task FromHexAsync_Invalid_ReturnsValidation()
    {
        var r = await _service.FromHexAsync("not-hex", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task FromHslAsync_Red_ReturnsRgb()
    {
        var r = await _service.FromHslAsync(0, 100, 50, TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(255, r.Value!.Red);
        Assert.Equal(0, r.Value.Green);
        Assert.Equal(0, r.Value.Blue);
    }

    [Fact]
    public async Task FromHslAsync_OutOfRange_ReturnsValidation()
    {
        var r = await _service.FromHslAsync(400, 100, 50, TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task RandomAsync_ReturnsValid()
    {
        var r = await _service.RandomAsync(TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.InRange(r.Value!.Red, 0, 255);
    }
}