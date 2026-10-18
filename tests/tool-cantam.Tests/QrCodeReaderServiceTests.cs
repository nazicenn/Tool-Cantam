using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class QrCodeReaderServiceTests
{
    private readonly QrCodeReaderService _service = new();

    private static string CreateQrPng(string text)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10);
        var path = Path.Combine(Path.GetTempPath(), $"qr_{System.Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public async Task ReadAsync_ValidQr_ReturnsText()
    {
        var p = CreateQrPng("hello-qr");
        try
        {
            var r = await _service.ReadAsync(p, CancellationToken.None);
            Assert.True(r.IsSuccess);
            Assert.Equal("hello-qr", r.Value!.Text);
        }
        finally { File.Delete(p); }
    }

    [Fact]
    public async Task ReadAsync_NullPath_ReturnsValidation()
    {
        var r = await _service.ReadAsync(null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_ReturnsNotFound()
    {
        var r = await _service.ReadAsync(@"C:\no_qr_xyz.png", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task ReadAsync_UnsupportedExtension_ReturnsInvalidFormat()
    {
        var p = Path.Combine(Path.GetTempPath(), $"qr_{System.Guid.NewGuid():N}.txt");
        File.WriteAllText(p, "x");
        try
        {
            var r = await _service.ReadAsync(p, CancellationToken.None);
            Assert.False(r.IsSuccess);
            Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
        }
        finally { File.Delete(p); }
    }
}