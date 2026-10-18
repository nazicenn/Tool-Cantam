using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MetadataExtractor;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>MetadataExtractor ile EXIF/XMP/IPTC/GPS okur.</summary>
public sealed class ExifViewerService : IExifViewerService
{
    public const long MaxFileSizeLimit = 100L * 1024 * 1024;
    private static readonly string[] Extensions =
        [".jpg", ".jpeg", ".png", ".tiff", ".tif", ".webp", ".gif", ".bmp", ".heic"];

    public long MaxFileSize => MaxFileSizeLimit;
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public Task<ToolResult<ExifReport>> ReadAsync(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail<ExifReport>(ToolErrorKind.Validation, ExifViewerErrorCodes.PathRequired, "Path is empty.");
        if (!File.Exists(path))
            return Fail<ExifReport>(ToolErrorKind.NotFound, ExifViewerErrorCodes.FileNotFound, $"File not found: {path}");

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!Extensions.Contains(ext))
            return Fail<ExifReport>(ToolErrorKind.InvalidFormat, ExifViewerErrorCodes.UnsupportedFormat, $"Unsupported: {ext}");

        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxFileSizeLimit)
                return Fail<ExifReport>(ToolErrorKind.TooLarge, ExifViewerErrorCodes.FileTooLarge, "File too large.");

            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var dirs = ImageMetadataReader.ReadMetadata(path);
            var directories = new List<ExifDirectory>();
            int total = 0;

            foreach (var dir in dirs)
            {
                ct.ThrowIfCancellationRequested();
                var tags = new List<ExifTag>();
                foreach (var tag in dir.Tags)
                {
                    tags.Add(new ExifTag(tag.Name, tag.Description ?? ""));
                    total++;
                }
                if (tags.Count > 0)
                    directories.Add(new ExifDirectory(dir.Name, tags));
            }

            sw.Stop();
            return Task.FromResult(ToolResult<ExifReport>.Ok(new ExifReport(
                path, info.Length, ext.TrimStart('.').ToUpperInvariant(),
                directories, total, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<ExifReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (ImageProcessingException ex) { return Fail<ExifReport>(ToolErrorKind.InvalidFormat, ExifViewerErrorCodes.CorruptImage, ex.Message); }
        catch (IOException ex) { return Fail<ExifReport>(ToolErrorKind.IoError, ExifViewerErrorCodes.IoError, ex.Message); }
        catch (Exception ex) { return Fail<ExifReport>(ToolErrorKind.Unexpected, ExifViewerErrorCodes.Unexpected, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}