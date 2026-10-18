using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>QR kod üretim sözleşmesi.</summary>
public interface IQrCodeService
{
    int MaxInputLength { get; }

    Task<ToolResult<QrCodeReport>> GenerateAsync(
        string? input,
        QrCodeEcc ecc,
        int pixelsPerModule,
        CancellationToken cancellationToken);
}

/// <summary>Hata düzeltme seviyesi.</summary>
public enum QrCodeEcc
{
    L, M, Q, H
}

/// <summary>Üretim raporu.</summary>
public sealed record QrCodeReport(
    string Input,
    QrCodeEcc Ecc,
    int PixelsPerModule,
    byte[] PngBytes,
    int ByteLength);