using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>QR kod okuma sözleşmesi.</summary>
public interface IQrCodeReaderService
{
    long MaxFileSize { get; }
    IReadOnlyList<string> SupportedExtensions { get; }

    Task<ToolResult<QrCodeReadReport>> ReadAsync(string? path, CancellationToken cancellationToken);
}

/// <summary>Okuma raporu.</summary>
public sealed record QrCodeReadReport(
    string FilePath,
    string Text,
    string Format,
    long FileSize);