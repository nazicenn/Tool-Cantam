using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class RsaKeyGeneratorServiceTests
{
    private readonly RsaKeyGeneratorService _service = new();

    [Fact]
    public async Task GenerateAsync_Pem_Works()
    {
        var r = await _service.GenerateAsync(new RsaOptions(2048, RsaFormat.Pem), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("BEGIN PUBLIC KEY", r.Value!.PublicKey);
        Assert.Contains("BEGIN PRIVATE KEY", r.Value.PrivateKey);
    }

    [Fact]
    public async Task GenerateAsync_Xml_Works()
    {
        var r = await _service.GenerateAsync(new RsaOptions(2048, RsaFormat.Xml), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("<RSAKeyValue>", r.Value!.PublicKey);
    }

    [Fact]
    public async Task GenerateAsync_Base64_Works()
    {
        var r = await _service.GenerateAsync(new RsaOptions(2048, RsaFormat.Base64), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(r.Value!.PublicKey));
    }

    [Fact]
    public async Task GenerateAsync_InvalidSize_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(new RsaOptions(1024, RsaFormat.Pem), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }
}