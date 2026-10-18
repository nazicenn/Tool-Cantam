using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ICertificateDecoderService
{
    int MaxInputLength { get; }
    Task<ToolResult<CertificateReport>> DecodeAsync(string? input, CancellationToken ct);
    Task<ToolResult<CertificateReport>> LoadFromFileAsync(string? path, CancellationToken ct);
}

public sealed record CertExtension(string Name, string Value);

public sealed record CertificateReport(
    string Version, string SerialNumber, string SignatureAlgorithm,
    string Issuer, string Subject,
    DateTime ValidFrom, DateTime ValidTo, bool IsCurrentlyValid,
    string PublicKeyAlgorithm, int PublicKeySize,
    string Thumbprint,
    IReadOnlyList<CertExtension> Extensions,
    TimeSpan Elapsed);