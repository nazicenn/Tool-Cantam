using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class QrCodeServiceTests
{
    private readonly QrCodeService _service = new();

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsPng()
    {
        var r = await _service.GenerateAsync("hello", QrCodeEcc.Q, 5, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.NotEmpty(r.Value!.PngBytes);
    }

    [Fact]
    public async Task GenerateAsync_Empty_ReturnsValidation()
    {
        var r = await _service.GenerateAsync("", QrCodeEcc.Q, 5, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_TooLong_ReturnsTooLarge()
    {
        var input = new string('x', QrCodeService.MaxInputLengthLimit + 1);
        var r = await _service.GenerateAsync(input, QrCodeEcc.Q, 5, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidPixels_ReturnsValidation()
    {
        var r = await _service.GenerateAsync("x", QrCodeEcc.Q, 0, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_Cancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.GenerateAsync("x", QrCodeEcc.Q, 5, cts.Token);
        Assert.False(r.IsSuccess);
    }
}