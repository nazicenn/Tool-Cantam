using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IPemViewerService
{
    int MaxInputLength { get; }
    Task<ToolResult<PemReport>> ParseAsync(string? input, CancellationToken ct);
}

public sealed record PemReport(
    string Subject, string Issuer, string SerialNumber,
    DateTime ValidFrom, DateTime ValidTo, bool IsCurrentlyValid,
    string PublicKey, string SignatureAlgorithm, string Thumbprint,
    IReadOnlyList<PemBlock> Blocks, TimeSpan Elapsed);

public sealed record PemBlock(string Label, int ByteCount);