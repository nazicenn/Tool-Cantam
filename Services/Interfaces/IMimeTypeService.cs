using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IMimeTypeService
{
    int MaxInputLength { get; }
    Task<ToolResult<MimeReport>> LookupByExtensionAsync(string? extension, CancellationToken ct);
    Task<ToolResult<MimeReport>> LookupByMimeAsync(string? mime, CancellationToken ct);
    IReadOnlyList<string> AllExtensions { get; }
    IReadOnlyList<string> AllMimeTypes { get; }
}

public sealed record MimeReport(
    string Input,
    string MimeType,
    string Description,
    string Category,
    bool IsText,
    bool IsCompressed,
    bool IsImage,
    bool IsAudio,
    bool IsVideo,
    bool IsArchive,
    TimeSpan Elapsed);