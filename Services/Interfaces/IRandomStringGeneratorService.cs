using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>Kriptografik güvenli rastgele metin üretim sözleşmesi.</summary>
public interface IRandomStringGeneratorService
{
    int MinLength { get; }
    int MaxLength { get; }
    int MinCount { get; }
    int MaxCount { get; }

    Task<ToolResult<RandomStringReport>> GenerateAsync(
        RandomStringOptions options, CancellationToken cancellationToken);
}

public sealed record RandomStringOptions(
    int Length,
    int Count,
    bool IncludeUppercase,
    bool IncludeLowercase,
    bool IncludeDigits,
    bool IncludeSymbols,
    bool ExcludeSimilar,
    bool EnsureEachType);

public sealed record RandomStringReport(
    IReadOnlyList<string> Items,
    string Combined,
    TimeSpan Elapsed,
    int EntropyBits);