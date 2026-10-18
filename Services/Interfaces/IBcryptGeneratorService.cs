using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IBcryptGeneratorService
{
    int MinWorkFactor { get; }
    int MaxWorkFactor { get; }
    Task<ToolResult<BcryptHashReport>> HashAsync(string? password, int workFactor, CancellationToken ct);
    Task<ToolResult<BcryptVerifyReport>> VerifyAsync(string? password, string? hash, CancellationToken ct);
    string GeneratePassword(int length = 16);
}

public sealed record BcryptHashReport(string Hash, int WorkFactor, TimeSpan Elapsed);
public sealed record BcryptVerifyReport(bool IsMatch, TimeSpan Elapsed);