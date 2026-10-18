using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IExifViewerService
{
    long MaxFileSize { get; }
    IReadOnlyList<string> SupportedExtensions { get; }
    Task<ToolResult<ExifReport>> ReadAsync(string? path, CancellationToken ct);
}

public sealed record ExifTag(string Name, string Value);
public sealed record ExifDirectory(string Name, IReadOnlyList<ExifTag> Tags);
public sealed record ExifReport(
    string FilePath, long FileSize, string Format,
    IReadOnlyList<ExifDirectory> Directories,
    int TotalTags, TimeSpan Elapsed);