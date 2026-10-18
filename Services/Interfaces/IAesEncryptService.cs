using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IAesEncryptService
{
    int MinPasswordLength { get; }
    Task<ToolResult<AesReport>> EncryptAsync(string? plaintext, string? password, CancellationToken ct);
    Task<ToolResult<AesReport>> DecryptAsync(string? ciphertextBase64, string? password, CancellationToken ct);
    string GeneratePassword(int length = 16);
}

public sealed record AesReport(string Output, int InputLength, int OutputLength, TimeSpan Elapsed);