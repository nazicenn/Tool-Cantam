using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ISecretGeneratorService
{
    int MinLength { get; }
    int MaxLength { get; }
    Task<ToolResult<SecretReport>> GenerateAsync(SecretOptions options, CancellationToken ct);
}

public enum SecretKind { ApiKey, JwtSecret, RandomToken, Password, HexKey }

public sealed record SecretOptions(int Length, SecretKind Kind, int Count);

public sealed record SecretReport(IReadOnlyList<string> Items, string Combined, int Length, TimeSpan Elapsed);