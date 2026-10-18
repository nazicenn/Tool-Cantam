using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class CertificateDecoderServiceTests
{
    private readonly CertificateDecoderService _service = new();

    private static string CreateSelfSignedPem()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test.example.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(System.DateTimeOffset.UtcNow.AddDays(-1), System.DateTimeOffset.UtcNow.AddDays(365));
        return cert.ExportCertificatePem();
    }

    [Fact]
    public async Task DecodeAsync_ValidPem_ReturnsReport()
    {
        var pem = CreateSelfSignedPem();
        var r = await _service.DecodeAsync(pem, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("test.example.com", r.Value!.Subject);
        Assert.True(r.Value.IsCurrentlyValid);
        Assert.Equal(2048, r.Value.PublicKeySize);
    }

    [Fact]
    public async Task DecodeAsync_Base64Only_ReturnsReport()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=x", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(System.DateTimeOffset.UtcNow.AddDays(-1), System.DateTimeOffset.UtcNow.AddDays(365));
        var b64 = System.Convert.ToBase64String(cert.RawData);
        var r = await _service.DecodeAsync(b64, CancellationToken.None);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public async Task DecodeAsync_Empty_ReturnsValidation()
    {
        var r = await _service.DecodeAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task DecodeAsync_Invalid_ReturnsInvalidFormat()
    {
        var r = await _service.DecodeAsync("not a certificate", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public async Task LoadFromFileAsync_Missing_ReturnsNotFound()
    {
        var r = await _service.LoadFromFileAsync(@"C:\nonexistent_cert_xyz.pem", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }
}