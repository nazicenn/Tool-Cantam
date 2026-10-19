using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Uzantı ↔ MIME türü araması. 100+ yaygın tür, NuGet yok.</summary>
public sealed class MimeTypeService : IMimeTypeService
{
    public const int MaxInputLengthLimit = 256;
    public int MaxInputLength => MaxInputLengthLimit;

    // Uzantı → (MIME, Açıklama, Kategori)
    private static readonly Dictionary<string, (string Mime, string Desc, string Cat)> ExtToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text
        { "txt", ("text/plain", "Plain text", "Text") },
        { "html", ("text/html", "HTML document", "Text") },
        { "htm", ("text/html", "HTML document", "Text") },
        { "css", ("text/css", "CSS stylesheet", "Text") },
        { "csv", ("text/csv", "CSV data", "Text") },
        { "xml", ("application/xml", "XML document", "Text") },
        { "json", ("application/json", "JSON data", "Text") },
        { "md", ("text/markdown", "Markdown document", "Text") },
        { "yaml", ("application/x-yaml", "YAML document", "Text") },
        { "yml", ("application/x-yaml", "YAML document", "Text") },
        { "toml", ("application/toml", "TOML document", "Text") },
        { "ini", ("text/plain", "INI config", "Text") },
        { "rtf", ("application/rtf", "Rich Text Format", "Text") },
        { "js", ("application/javascript", "JavaScript", "Text") },
        { "mjs", ("application/javascript", "JavaScript module", "Text") },
        { "ts", ("application/typescript", "TypeScript", "Text") },
        { "jsx", ("text/jsx", "JSX", "Text") },
        { "tsx", ("text/tsx", "TSX", "Text") },
        { "c", ("text/x-c", "C source", "Text") },
        { "h", ("text/x-c", "C header", "Text") },
        { "cpp", ("text/x-c++", "C++ source", "Text") },
        { "cs", ("text/x-csharp", "C# source", "Text") },
        { "java", ("text/x-java-source", "Java source", "Text") },
        { "py", ("text/x-python", "Python source", "Text") },
        { "rb", ("text/x-ruby", "Ruby source", "Text") },
        { "go", ("text/x-go", "Go source", "Text") },
        { "rs", ("text/x-rust", "Rust source", "Text") },
        { "php", ("application/x-httpd-php", "PHP source", "Text") },
        { "sh", ("application/x-sh", "Shell script", "Text") },
        { "bat", ("application/x-bat", "Batch script", "Text") },
        { "ps1", ("application/x-powershell", "PowerShell script", "Text") },

        // Images
        { "png", ("image/png", "PNG image", "Image") },
        { "jpg", ("image/jpeg", "JPEG image", "Image") },
        { "jpeg", ("image/jpeg", "JPEG image", "Image") },
        { "gif", ("image/gif", "GIF image", "Image") },
        { "bmp", ("image/bmp", "BMP image", "Image") },
        { "webp", ("image/webp", "WebP image", "Image") },
        { "svg", ("image/svg+xml", "SVG vector image", "Image") },
        { "ico", ("image/x-icon", "Icon", "Image") },
        { "tif", ("image/tiff", "TIFF image", "Image") },
        { "tiff", ("image/tiff", "TIFF image", "Image") },
        { "avif", ("image/avif", "AVIF image", "Image") },
        { "heic", ("image/heic", "HEIC image", "Image") },

        // Audio
        { "mp3", ("audio/mpeg", "MP3 audio", "Audio") },
        { "wav", ("audio/wav", "WAV audio", "Audio") },
        { "ogg", ("audio/ogg", "OGG audio", "Audio") },
        { "flac", ("audio/flac", "FLAC audio", "Audio") },
        { "aac", ("audio/aac", "AAC audio", "Audio") },
        { "m4a", ("audio/mp4", "M4A audio", "Audio") },
        { "opus", ("audio/opus", "Opus audio", "Audio") },

        // Video
        { "mp4", ("video/mp4", "MP4 video", "Video") },
        { "webm", ("video/webm", "WebM video", "Video") },
        { "mkv", ("video/x-matroska", "Matroska video", "Video") },
        { "avi", ("video/x-msvideo", "AVI video", "Video") },
        { "mov", ("video/quicktime", "QuickTime video", "Video") },
        { "wmv", ("video/x-ms-wmv", "WMV video", "Video") },
        { "flv", ("video/x-flv", "Flash video", "Video") },
        { "m4v", ("video/x-m4v", "M4V video", "Video") },

        // Archives
        { "zip", ("application/zip", "ZIP archive", "Archive") },
        { "rar", ("application/vnd.rar", "RAR archive", "Archive") },
        { "7z", ("application/x-7z-compressed", "7-Zip archive", "Archive") },
        { "tar", ("application/x-tar", "TAR archive", "Archive") },
        { "gz", ("application/gzip", "GZIP compressed", "Archive") },
        { "bz2", ("application/x-bzip2", "BZIP2 compressed", "Archive") },
        { "xz", ("application/x-xz", "XZ compressed", "Archive") },
        { "zst", ("application/zstd", "Zstandard compressed", "Archive") },

        // Documents
        { "pdf", ("application/pdf", "PDF document", "Document") },
        { "doc", ("application/msword", "Word document", "Document") },
        { "docx", ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Word document", "Document") },
        { "xls", ("application/vnd.ms-excel", "Excel spreadsheet", "Document") },
        { "xlsx", ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Excel spreadsheet", "Document") },
        { "ppt", ("application/vnd.ms-powerpoint", "PowerPoint presentation", "Document") },
        { "pptx", ("application/vnd.openxmlformats-officedocument.presentationml.presentation", "PowerPoint presentation", "Document") },
        { "odt", ("application/vnd.oasis.opendocument.text", "OpenDocument text", "Document") },
        { "ods", ("application/vnd.oasis.opendocument.spreadsheet", "OpenDocument spreadsheet", "Document") },
        { "epub", ("application/epub+zip", "EPUB book", "Document") },

        // Fonts
        { "ttf", ("font/ttf", "TrueType font", "Font") },
        { "otf", ("font/otf", "OpenType font", "Font") },
        { "woff", ("font/woff", "WOFF font", "Font") },
        { "woff2", ("font/woff2", "WOFF2 font", "Font") },

        // Executables
        { "exe", ("application/vnd.microsoft.portable-executable", "Windows executable", "Executable") },
        { "dll", ("application/vnd.microsoft.portable-executable", "Windows library", "Executable") },
        { "msi", ("application/x-msi", "Windows installer", "Executable") },
        { "apk", ("application/vnd.android.package-archive", "Android package", "Executable") },
        { "deb", ("application/vnd.debian.binary-package", "Debian package", "Executable") },
        { "rpm", ("application/x-rpm", "RPM package", "Executable") },
        { "dmg", ("application/x-apple-diskimage", "macOS disk image", "Executable") },
        { "iso", ("application/x-iso9660-image", "ISO disc image", "Executable") },

        // Web
        { "wasm", ("application/wasm", "WebAssembly", "Web") },
        { "manifest", ("text/cache-manifest", "Web manifest", "Web") },
        { "appcache", ("text/cache-manifest", "AppCache manifest", "Web") },
        { "map", ("application/json", "Source map", "Web") },
    };

    private static readonly Dictionary<string, string> MimeToExt = new(StringComparer.OrdinalIgnoreCase);

    static MimeTypeService()
    {
        foreach (var kv in ExtToMime)
        {
            if (!MimeToExt.ContainsKey(kv.Value.Mime))
                MimeToExt[kv.Value.Mime] = kv.Key;
        }
    }

    public IReadOnlyList<string> AllExtensions => ExtToMime.Keys.OrderBy(k => k).ToList();
    public IReadOnlyList<string> AllMimeTypes => MimeToExt.Keys.OrderBy(k => k).ToList();

    public Task<ToolResult<MimeReport>> LookupByExtensionAsync(string? extension, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return Fail<MimeReport>(ToolErrorKind.Validation, MimeTypeErrorCodes.EmptyInput, "Extension is empty.");

        var ext = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (ext.Length > MaxInputLengthLimit)
            return Fail<MimeReport>(ToolErrorKind.TooLarge, MimeTypeErrorCodes.InvalidExtension, "Extension too long.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            if (!ExtToMime.TryGetValue(ext, out var info))
                return Fail<MimeReport>(ToolErrorKind.NotFound, MimeTypeErrorCodes.NotFound, $"Unknown extension: {ext}");

            sw.Stop();
            var report = Build(extension, info.Mime, info.Desc, info.Cat, sw.Elapsed);
            return Task.FromResult(ToolResult<MimeReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<MimeReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<MimeReport>(ToolErrorKind.Unexpected, MimeTypeErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<MimeReport>> LookupByMimeAsync(string? mime, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mime))
            return Fail<MimeReport>(ToolErrorKind.Validation, MimeTypeErrorCodes.EmptyInput, "MIME is empty.");

        var m = mime.Trim().ToLowerInvariant();
        if (m.Length > MaxInputLengthLimit)
            return Fail<MimeReport>(ToolErrorKind.TooLarge, MimeTypeErrorCodes.InvalidExtension, "MIME too long.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            if (!MimeToExt.TryGetValue(m, out var ext))
                return Fail<MimeReport>(ToolErrorKind.NotFound, MimeTypeErrorCodes.NotFound, $"Unknown MIME: {m}");

            var info = ExtToMime[ext];
            sw.Stop();
            var report = Build(mime, info.Mime, info.Desc, info.Cat, sw.Elapsed);
            return Task.FromResult(ToolResult<MimeReport>.Ok(report));
        }
        catch (OperationCanceledException) { return Fail<MimeReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<MimeReport>(ToolErrorKind.Unexpected, MimeTypeErrorCodes.Unexpected, ex.Message); }
    }

    private static MimeReport Build(string input, string mime, string desc, string cat, TimeSpan elapsed)
    {
        return new MimeReport(
            input, mime, desc, cat,
            IsText: cat == "Text" || mime.StartsWith("text/") || mime == "application/json" || mime == "application/xml",
            IsCompressed: mime.Contains("zip") || mime.Contains("gzip") || mime.Contains("bzip") || mime.Contains("xz") || mime.Contains("zstd"),
            IsImage: mime.StartsWith("image/"),
            IsAudio: mime.StartsWith("audio/"),
            IsVideo: mime.StartsWith("video/"),
            IsArchive: cat == "Archive",
            elapsed);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}