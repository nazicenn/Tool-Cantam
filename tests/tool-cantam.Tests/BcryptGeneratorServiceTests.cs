using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class BcryptGeneratorServiceTests
{
    private readonly BcryptGeneratorService _service = new();

    [Fact]
    public async Task HashAsync_Valid_ReturnsHash()
    {
        var r = await _service.HashAsync("secret", 10, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.StartsWith("$2", r.Value!.Hash);
    }

    [Fact]
    public async Task HashAsync_Empty_ReturnsValidation()
    {
        var r = await _service.HashAsync("", 10, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task HashAsync_InvalidWork_ReturnsValidation()
    {
        var r = await _service.HashAsync("x", 3, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task VerifyAsync_Match_True()
    {
        var h = await _service.HashAsync("secret", 10, CancellationToken.None);
        var v = await _service.VerifyAsync("secret", h.Value!.Hash, CancellationToken.None);
        Assert.True(v.IsSuccess);
        Assert.True(v.Value!.IsMatch);
    }

    [Fact]
    public async Task VerifyAsync_Mismatch_False()
    {
        var h = await _service.HashAsync("secret", 10, CancellationToken.None);
        var v = await _service.VerifyAsync("wrong", h.Value!.Hash, CancellationToken.None);
        Assert.True(v.IsSuccess);
        Assert.False(v.Value!.IsMatch);
    }

    [Fact]
    public async Task VerifyAsync_InvalidHash_ReturnsInvalidFormat()
    {
        var v = await _service.VerifyAsync("x", "not-a-bcrypt-hash", CancellationToken.None);
        Assert.False(v.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, v.ErrorKind);
    }

    [Fact]
    public void GeneratePassword_Length()
    {
        var p = _service.GeneratePassword(16);
        Assert.Equal(16, p.Length);
    }
}