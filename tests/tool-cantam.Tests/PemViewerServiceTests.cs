using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class PemViewerServiceTests
{
    private readonly PemViewerService _service = new();

    private static string CreatePem()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = req.CreateSelfSigned(System.DateTimeOffset.UtcNow.AddDays(-1), System.DateTimeOffset.UtcNow.AddDays(365));
        return cert.ExportCertificatePem();
    }

    [Fact]
    public async Task ParseAsync_ValidPem_ReturnsReport()
    {
        var r = await _service.ParseAsync(CreatePem(), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("test", r.Value!.Subject);
        Assert.Single(r.Value.Blocks);
        Assert.True(r.Value.IsCurrentlyValid);
    }

    [Fact]
    public async Task ParseAsync_Empty_ReturnsValidation()
    {
        var r = await _service.ParseAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ParseAsync_NoCertificate_ReturnsInvalidFormat()
    {
        var r = await _service.ParseAsync("-----BEGIN PUBLIC KEY-----\nMFwwDQYJKoZIhvcNAQEBBQADSwAwSAJB\n-----END PUBLIC KEY-----", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }
}