using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IGitCommandGeneratorService
{
    int MaxInputLength { get; }
    IReadOnlyList<GitCommandCategory> Categories { get; }
    Task<ToolResult<GitCommandReport>> GenerateAsync(GitCommandOptions options, CancellationToken ct);
}

public enum GitCommandCategory
{
    Commit,
    Branch,
    Merge,
    Rebase,
    Reset,
    Tag,
    Stash,
    Remote,
    Log
}

public sealed record GitCommandOptions(
    GitCommandCategory Category,
    string Argument1,
    string Argument2,
    bool Flag1,
    bool Flag2);

public sealed record GitCommandReport(
    string Command,
    string Description,
    string Category,
    TimeSpan Elapsed);