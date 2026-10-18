using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class SecretGeneratorServiceTests
{
    private readonly SecretGeneratorService _service = new();

    private static SecretOptions Opts(int len = 32, SecretKind kind = SecretKind.ApiKey, int count = 1)
        => new(len, kind, count);

    [Fact]
    public async Task GenerateAsync_ApiKey_ReturnsUpperAlphanum()
    {
        var r = await _service.GenerateAsync(Opts(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Single(r.Value!.Items);
        Assert.Equal(32, r.Value.Items[0].Length);
        Assert.All(r.Value.Items[0], c => Assert.True(char.IsUpper(c) || char.IsDigit(c)));
    }

    [Fact]
    public async Task GenerateAsync_Password_IncludesSymbols()
    {
        var r = await _service.GenerateAsync(Opts(kind: SecretKind.Password), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains(r.Value!.Items[0], c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public async Task GenerateAsync_HexKey_OnlyHex()
    {
        var r = await _service.GenerateAsync(Opts(kind: SecretKind.HexKey), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.All(r.Value!.Items[0], c => Assert.True(Uri.IsHexDigit(c)));
    }

    [Fact]
    public async Task GenerateAsync_Count3_ReturnsThree()
    {
        var r = await _service.GenerateAsync(Opts(count: 3), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.Items.Count);
    }

    [Fact]
    public async Task GenerateAsync_InvalidLength_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(len: 2), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidCount_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(count: 0), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}