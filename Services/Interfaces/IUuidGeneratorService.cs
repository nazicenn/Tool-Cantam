using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>UUID üretim sözleşmesi.</summary>
public interface IUuidGeneratorService
{
    int MinCount { get; }
    int MaxCount { get; }

    Task<ToolResult<UuidReport>> GenerateAsync(UuidOptions options, CancellationToken cancellationToken);
}

public enum UuidCase { Upper, Lower }
public enum UuidVersion { V4 }

public sealed record UuidOptions(
    int Count,
    UuidCase Case,
    bool IncludeHyphens,
    bool IncludeBraces);

public sealed record UuidReport(
    IReadOnlyList<string> Items,
    string Combined,
    TimeSpan Elapsed,
    UuidVersion Version);