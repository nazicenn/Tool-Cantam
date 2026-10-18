using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>URL-friendly slug üretim sözleşmesi.</summary>
public interface ISlugGeneratorService
{
    int MaxInputLength { get; }
    int MaxSlugLength { get; }

    Task<ToolResult<SlugReport>> GenerateAsync(
        string? input, SlugOptions options, CancellationToken cancellationToken);
}

public sealed record SlugOptions(
    bool ToLowercase,
    char Separator,
    int MaxLength,
    bool AllowUnicode);

public sealed record SlugReport(
    string Slug,
    int OriginalLength,
    int SlugLength,
    TimeSpan Elapsed);