using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// WPF BitmapMetadata/BitmapEncoder ile metadata okur ve temizler. NuGet bağımlılığı yoktur.
/// Geçici dosya + atomik taşıma kullanılır.
/// </summary>
public sealed class MetadataService : IMetadataService
{
    public const long MaxFileSizeLimit = 100L * 1024 * 1024; // 100 MB

    private static readonly string[] Extensions =
        [".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif"];

    public long MaxFileSize => MaxFileSizeLimit;
    public IReadOnlyList<string> SupportedExtensions => Extensions;

    public async Task<ToolResult<MetadataReport>> AnalyzeAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.Validation, MetadataErrorCodes.PathRequired, "Path is null or empty.");

        if (!File.Exists(path))
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.NotFound, MetadataErrorCodes.FileNotFound, $"File not found: {path}");

        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!Extensions.Contains(ext))
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.InvalidFormat, MetadataErrorCodes.UnsupportedFormat, $"Unsupported extension: {ext}");

        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxFileSizeLimit)
                return ToolResult<MetadataReport>.Fail(ToolErrorKind.TooLarge, MetadataErrorCodes.FileTooLarge,
                    $"File size {info.Length} exceeds {MaxFileSizeLimit}.");

            cancellationToken.ThrowIfCancellationRequested();

            // WPF decoder'ı UI thread'de çalışmalı; Task.Run içine alıyoruz.
            var payload = await Task.Run(() =>
            {
                var frame = LoadFrame(path);
                var entries = new List<MetadataEntry>();
                bool hasExif = false, hasGps = false, hasXmp = false, hasIptc = false, hasComments = false, hasThumb = false;

                if (frame.Metadata is BitmapMetadata meta)
                {
                    CollectEntries(meta, entries, ref hasExif, ref hasGps, ref hasXmp, ref hasIptc, ref hasComments);
                    try
                    {
                        var thumb = meta.GetQuery("/app1/ifd/thumbnail") as BitmapMetadata;
                        hasThumb = thumb is not null;
                    }
                    catch { /* yoksay */ }
                }

                return new MetadataReport(
                    path, info.Length, ext.TrimStart('.').ToUpperInvariant(),
                    entries, hasExif, hasGps, hasXmp, hasIptc, hasComments, hasThumb);
            }, cancellationToken).ConfigureAwait(false);

            return ToolResult<MetadataReport>.Ok(payload);
        }
        catch (OperationCanceledException)
        {
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (NotSupportedException ex)
        {
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.InvalidFormat, MetadataErrorCodes.CorruptImage, ex.Message);
        }
        catch (FileFormatException ex)
        {
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.InvalidFormat, MetadataErrorCodes.CorruptImage, ex.Message);
        }
        catch (IOException ex)
        {
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.IoError, MetadataErrorCodes.IoError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<MetadataReport>.Fail(ToolErrorKind.Unexpected, MetadataErrorCodes.Unexpected, ex.Message);
        }
    }

    public async Task<ToolResult<CleanReport>> CleanAsync(
        string? sourcePath,
        string? destinationPath,
        MetadataCleanOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return ToolResult<CleanReport>.Fail(ToolErrorKind.Validation, MetadataErrorCodes.PathRequired, "Source path is null.");
        if (string.IsNullOrWhiteSpace(destinationPath))
            return ToolResult<CleanReport>.Fail(ToolErrorKind.Validation, MetadataErrorCodes.PathRequired, "Destination path is null.");
        if (!File.Exists(sourcePath))
            return ToolResult<CleanReport>.Fail(ToolErrorKind.NotFound, MetadataErrorCodes.FileNotFound, $"File not found: {sourcePath}");
        if (options is null || !options.Any)
            return ToolResult<CleanReport>.Fail(ToolErrorKind.Validation, MetadataErrorCodes.NoOptionsSelected, "No metadata types selected.");

        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!Extensions.Contains(ext))
            return ToolResult<CleanReport>.Fail(ToolErrorKind.InvalidFormat, MetadataErrorCodes.UnsupportedFormat, $"Unsupported extension: {ext}");

        var tempPath = destinationPath + ".tmp";
        var sw = Stopwatch.StartNew();

        try
        {
            var report = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var originalBytes = new FileInfo(sourcePath).Length;
                int fieldsRemoved = CountRemoved(sourcePath, options);

                var frame = LoadFrame(sourcePath);
                var encoder = CreateEncoder(ext);
                encoder.Frames.Add(BitmapFrame.Create(frame)); // metadata yok → temiz

                using (var fs = File.Create(tempPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    encoder.Save(fs);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(tempPath, destinationPath, overwrite: true);

                var cleanedBytes = new FileInfo(destinationPath).Length;
                return new CleanReport(fieldsRemoved, originalBytes, cleanedBytes, sw.Elapsed);
            }, cancellationToken).ConfigureAwait(false);

            sw.Stop();
            return ToolResult<CleanReport>.Ok(report);
        }
        catch (OperationCanceledException)
        {
            return ToolResult<CleanReport>.Fail(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (IOException ex)
        {
            return ToolResult<CleanReport>.Fail(ToolErrorKind.IoError, MetadataErrorCodes.IoError, ex.Message);
        }
        catch (Exception ex)
        {
            return ToolResult<CleanReport>.Fail(ToolErrorKind.Unexpected, MetadataErrorCodes.Unexpected, ex.Message);
        }
        finally
        {
            sw.Stop();
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* ignore */ }
            }
        }
    }

    private static BitmapFrame LoadFrame(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        return decoder.Frames[0];
    }

    private static BitmapEncoder CreateEncoder(string ext) => ext switch
    {
        ".png" => new PngBitmapEncoder(),
        ".bmp" => new BmpBitmapEncoder(),
        ".tiff" or ".tif" => new TiffBitmapEncoder(),
        _ => new JpegBitmapEncoder { QualityLevel = 95 },
    };

    private static void CollectEntries(
        BitmapMetadata meta,
        List<MetadataEntry> entries,
        ref bool hasExif,
        ref bool hasGps,
        ref bool hasXmp,
        ref bool hasIptc,
        ref bool hasComments)
    {
        TryCollect("/app1/ifd", "EXIF", meta, entries, ref hasExif);
        TryCollect("/app1/ifd/gps", "GPS", meta, entries, ref hasGps);
        TryCollect("/xmp", "XMP", meta, entries, ref hasXmp);
        TryCollect("/app13/irb/8bimiptc", "IPTC", meta, entries, ref hasIptc);
        TryCollect("/app1/ifd/exif", "EXIF", meta, entries, ref hasExif);
        TryCollect("/app1/ifd/thumb", "Thumb", meta, entries, ref hasExif);

        if (meta.Comment is { Length: > 0 } c)
        {
            entries.Add(new MetadataEntry("Comment", "Comment", c));
            hasComments = true;
        }
    }

    private static void TryCollect(
        string query,
        string dir,
        BitmapMetadata meta,
        List<MetadataEntry> entries,
        ref bool has)
    {
        try
        {
            if (meta.GetQuery(query) is BitmapMetadata sub)
            {
                foreach (var tag in sub)
                {
                    try
                    {
                        var val = sub.GetQuery(tag);
                        if (val is not null)
                            entries.Add(new MetadataEntry(dir, tag, val.ToString() ?? string.Empty));
                    }
                    catch { /* yoksay */ }
                }
                has = has || entries.Any(e => e.Directory == dir);
            }
        }
        catch { /* yoksay */ }
    }

    private static int CountRemoved(string path, MetadataCleanOptions options)
    {
        try
        {
            var frame = LoadFrame(path);
            if (frame.Metadata is not BitmapMetadata meta) return 0;

            var entries = new List<MetadataEntry>();
            bool exif = false, gps = false, xmp = false, iptc = false, cmt = false;
            CollectEntries(meta, entries, ref exif, ref gps, ref xmp, ref iptc, ref cmt);

            int removed = 0;
            if (options.Exif && exif) removed += entries.Count(e => e.Directory == "EXIF");
            if (options.Gps && gps) removed += entries.Count(e => e.Directory == "GPS");
            if (options.Xmp && xmp) removed += entries.Count(e => e.Directory == "XMP");
            if (options.Iptc && iptc) removed += entries.Count(e => e.Directory == "IPTC");
            if (options.Comments && cmt) removed += entries.Count(e => e.Directory == "Comment");
            if (options.Thumbnail) removed += entries.Count(e => e.Directory == "Thumb");
            return removed;
        }
        catch { return 0; }
    }
}