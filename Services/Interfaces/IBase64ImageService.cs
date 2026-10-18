using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IBase64ImageService
{
    long MaxFileSize { get; }
    Task<ToolResult<ImageToBase64Report>> ImageToBase64Async(string? path, CancellationToken ct);
    Task<ToolResult<Base64ToImageReport>> Base64ToImageAsync(string? base64, CancellationToken ct);
}

public sealed record ImageToBase64Report(string Base64, long OriginalBytes, string Format, TimeSpan Elapsed);
public sealed record Base64ToImageReport(byte[] ImageBytes, string DetectedFormat, int ByteCount);