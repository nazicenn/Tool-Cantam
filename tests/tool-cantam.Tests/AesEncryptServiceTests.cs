using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class AesEncryptServiceTests
{
    private readonly AesEncryptService _service = new();
    private const string Pwd = "strong-password-123";

    [Fact]
    public async Task EncryptDecrypt_RoundTrip()
    {
        var e = await _service.EncryptAsync("hello world", Pwd, CancellationToken.None);
        Assert.True(e.IsSuccess);
        var d = await _service.DecryptAsync(e.Value!.Output, Pwd, CancellationToken.None);
        Assert.True(d.IsSuccess);
        Assert.Equal("hello world", d.Value!.Output);
    }

    [Fact]
    public async Task Encrypt_EmptyPlain_ReturnsValidation()
    {
        var r = await _service.EncryptAsync("", Pwd, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task Encrypt_ShortPassword_ReturnsValidation()
    {
        var r = await _service.EncryptAsync("x", "short", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task Decrypt_InvalidBase64_ReturnsInvalidFormat()
    {
        var r = await _service.DecryptAsync("not-base64!!!", Pwd, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task Decrypt_WrongPassword_Fails()
    {
        var e = await _service.EncryptAsync("hello", Pwd, CancellationToken.None);
        var d = await _service.DecryptAsync(e.Value!.Output, "different-password-123", CancellationToken.None);
        Assert.False(d.IsSuccess);
    }

    [Fact]
    public void GeneratePassword_Length()
    {
        var p = _service.GeneratePassword(16);
        Assert.Equal(16, p.Length);
    }
}