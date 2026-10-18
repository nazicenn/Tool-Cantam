using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Görüntü dosyalarındaki EXIF/GPS/XMP/IPTC/yorum/thumbnail metadata'sını
/// analiz eden ve seçmeli olarak temizleyen servis sözleşmesi.
/// Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner; exception fırlatılmaz.
/// NuGet bağımlılığı yoktur; WPF BitmapMetadata/BitmapEncoder kullanır.
/// </summary>
public interface IMetadataService
{
    long MaxFileSize { get; }
    IReadOnlyList<string> SupportedExtensions { get; }

    Task<ToolResult<MetadataReport>> AnalyzeAsync(string? path, CancellationToken cancellationToken);
    Task<ToolResult<CleanReport>> CleanAsync(
        string? sourcePath,
        string? destinationPath,
        MetadataCleanOptions options,
        CancellationToken cancellationToken);
}

/// <summary>Temizlenecek metadata türleri.</summary>
public sealed record MetadataCleanOptions(
    bool Exif,
    bool Gps,
    bool Xmp,
    bool Iptc,
    bool Comments,
    bool Thumbnail)
{
    public bool Any => Exif || Gps || Xmp || Iptc || Comments || Thumbnail;
    public static MetadataCleanOptions All { get; } = new(true, true, true, true, true, true);
    public static MetadataCleanOptions None { get; } = new(false, false, false, false, false, false);
}

/// <summary>Tek bir metadata alanı.</summary>
public sealed record MetadataEntry(string Directory, string TagName, string Value);

/// <summary>Analiz raporu.</summary>
public sealed record MetadataReport(
    string FilePath,
    long FileSize,
    string Format,
    IReadOnlyList<MetadataEntry> Entries,
    bool HasExif,
    bool HasGps,
    bool HasXmp,
    bool HasIptc,
    bool HasComments,
    bool HasThumbnail);

/// <summary>Temizleme raporu.</summary>
public sealed record CleanReport(
    int FieldsRemoved,
    long OriginalBytes,
    long CleanedBytes,
    System.TimeSpan Elapsed);