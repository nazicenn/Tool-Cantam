using System;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// JWT (JSON Web Token) decode sözleşmesi. Sadece decode yapar, doğrulama (signature verify) yapmaz.
/// Referans: https://datatracker.ietf.org/doc/html/rfc7519
/// </summary>
public interface IJwtService
{
    int MaxTokenLength { get; }

    ToolResult<JwtReport> Decode(string? jwt);
}

/// <summary>JWT decode sonucu.</summary>
public sealed record JwtReport(
    string Header,
    string Payload,
    string Signature,
    string Algorithm,
    string TokenType,
    string? Issuer,
    string? Subject,
    string? Audience,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? NotBefore,
    DateTimeOffset? Expiration,
    string? JwtId,
    bool IsExpired,
    bool IsNotYetValid,
    TimeSpan? TimeUntilExpiry,
    TimeSpan Elapsed);