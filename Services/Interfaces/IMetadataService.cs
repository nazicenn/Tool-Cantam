using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Görüntü dosyalarından EXIF/metadata temizleme servis arayüzü.
/// Desteklenen formatlar: JPEG, PNG, BMP, TIFF.
/// Referans: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.bitmapencoder
/// </summary>
public interface IMetadataService
{
    Task<bool> PurgeExifAsync(string sourcePath, string destinationPath);
}
