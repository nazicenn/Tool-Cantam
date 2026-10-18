using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Resim ↔ Base64. PNG/JPEG/GIF/BMP/WEBP magic byte kontrolü.</summary>
public sealed class Base64ImageService : IBase64ImageService
{
    public const long MaxFileSizeLimit = 50L * 1024 * 1024;
    public long MaxFileSize => MaxFileSizeLimit;

    public Task<ToolResult<ImageToBase64Report>> ImageToBase64Async(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail<ImageToBase64Report>(ToolErrorKind.Validation, Base64ImageErrorCodes.PathRequired, "Path is empty.");
        if (!File.Exists(path))
            return Fail<ImageToBase64Report>(ToolErrorKind.NotFound, Base64ImageErrorCodes.FileNotFound, "File not found.");

        try
        {
            var info = new FileInfo(path);
            if (info.Length > MaxFileSizeLimit)
                return Fail<ImageToBase64Report>(ToolErrorKind.TooLarge, Base64ImageErrorCodes.FileTooLarge, "File too large.");

            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var bytes = File.ReadAllBytes(path);
            var format = DetectFormat(bytes);
            if (format is null)
                return Fail<ImageToBase64Report>(ToolErrorKind.InvalidFormat, Base64ImageErrorCodes.UnsupportedFormat, "Not a supported image.");

            var base64 = Convert.ToBase64String(bytes);
            sw.Stop();
            return Task.FromResult(ToolResult<ImageToBase64Report>.Ok(new ImageToBase64Report(base64, info.Length, format, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<ImageToBase64Report>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<ImageToBase64Report>(ToolErrorKind.Unexpected, Base64ImageErrorCodes.Unexpected, ex.Message); }
    }

    public Task<ToolResult<Base64ToImageReport>> Base64ToImageAsync(string? base64, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return Fail<Base64ToImageReport>(ToolErrorKind.Validation, Base64ImageErrorCodes.InvalidBase64, "Base64 is empty.");

        try
        {
            ct.ThrowIfCancellationRequested();
            // data URI prefix'i temizle
            var raw = base64.Trim();
            var idx = raw.IndexOf(',');
            if (raw.StartsWith("data:") && idx > 0) raw = raw[(idx + 1)..];

            byte[] bytes;
            try { bytes = Convert.FromBase64String(raw); }
            catch (FormatException) { return Fail<Base64ToImageReport>(ToolErrorKind.InvalidFormat, Base64ImageErrorCodes.InvalidBase64, "Invalid Base64."); }

            var format = DetectFormat(bytes);
            if (format is null)
                return Fail<Base64ToImageReport>(ToolErrorKind.InvalidFormat, Base64ImageErrorCodes.NotAnImage, "Not a recognized image format.");

            return Task.FromResult(ToolResult<Base64ToImageReport>.Ok(new Base64ToImageReport(bytes, format, bytes.Length)));
        }
        catch (OperationCanceledException) { return Fail<Base64ToImageReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<Base64ToImageReport>(ToolErrorKind.Unexpected, Base64ImageErrorCodes.Unexpected, ex.Message); }
    }

    private static string? DetectFormat(byte[] b)
    {
        if (b.Length < 12) return null;
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "PNG";
        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "JPEG";
        if (b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "GIF";
        if (b[0] == 0x42 && b[1] == 0x4D) return "BMP";
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "WEBP";
        return null;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}