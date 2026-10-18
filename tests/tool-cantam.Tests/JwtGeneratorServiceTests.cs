using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class JwtGeneratorServiceTests
{
    private readonly JwtGeneratorService _service = new();

    // 64 char key → HS256 (32), HS384 (48), HS512 (64) hepsi için yeterli
    private const string ValidKey =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static JwtOptions Opts(
        string key = ValidKey,
        string claims = "userId:1",
        int exp = 60,
        JwtAlgorithm a = JwtAlgorithm.HS256)
        => new(key, "app", "users", exp, claims, a);

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsToken()
    {
        var r = await _service.GenerateAsync(Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.Token.Split('.').Length);
    }

    [Fact]
    public async Task GenerateAsync_ShortKey_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(key: "short"), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_EmptyKey_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(key: ""), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidExpiry_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(exp: 0), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidClaims_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(claims: "no-colon-here"), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_DifferentAlgorithms_Work()
    {
        foreach (var a in new[] { JwtAlgorithm.HS256, JwtAlgorithm.HS384, JwtAlgorithm.HS512 })
        {
            var r = await _service.GenerateAsync(Opts(a: a), CancellationToken.None);
            Assert.True(r.IsSuccess, $"Failed for {a}: {r.ErrorCode}");
            Assert.Equal(a.ToString(), r.Value!.Algorithm);
        }
    }
}