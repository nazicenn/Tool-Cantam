using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IJwtGeneratorService
{
    int MinKeyLength { get; }
    int MaxKeyLength { get; }
    Task<ToolResult<JwtGenReport>> GenerateAsync(JwtOptions options, CancellationToken ct);
}

public enum JwtAlgorithm { HS256, HS384, HS512 }

public sealed record JwtOptions(
    string SecretKey,
    string Issuer,
    string Audience,
    int ExpiryMinutes,
    string Claims,
    JwtAlgorithm Algorithm);

public sealed record JwtGenReport(
    string Token, string Algorithm, DateTime ExpiresAt,
    int ClaimCount, int HeaderLength, int PayloadLength, int SignatureLength,
    TimeSpan Elapsed);