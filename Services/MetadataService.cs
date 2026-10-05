using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Görüntü dosyalarından EXIF/metadata temizleme servis uygulaması.
/// WPF BitmapImage/BitmapEncoder kullanarak piksel verisini yeniden kodlar,
/// böylece tüm metadata katmanları sıyrılır.
/// </summary>
public class MetadataService : IMetadataService
{
    /// <summary>Desteklenen görüntü uzantıları.</summary>
    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif"];

    public async Task<bool> PurgeExifAsync(string sourcePath, string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not found.", sourcePath);

        // Desteklenen format kontrolü
        string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!Array.Exists(SupportedExtensions, ext => ext == extension))
            throw new NotSupportedException(
                $"Unsupported file format: '{extension}'. Supported formats: {string.Join(", ", SupportedExtensions)}");

        string tempPath = destinationPath + ".tmp";

        try
        {
            return await Task.Run(() =>
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Belleğe al ki dosya kilitlenmesin
                bitmap.UriSource = new Uri(sourcePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                var encoder = GetEncoder(extension);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var stream = File.Create(tempPath))
                {
                    encoder.Save(stream);
                }

                File.Move(tempPath, destinationPath, overwrite: true);
                return true;
            }).ConfigureAwait(false);
        }
        finally
        {
            // Temizlik: Yarıda kalan tmp dosyasını sil (çöp koruması)
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* Temizlik hatası yoksayılır */ }
            }
        }
    }

    private static BitmapEncoder GetEncoder(string extension)
    {
        return extension switch
        {
            ".png"              => new PngBitmapEncoder(),
            ".bmp"              => new BmpBitmapEncoder(),
            ".tiff" or ".tif"   => new TiffBitmapEncoder(),
            _                   => new JpegBitmapEncoder { QualityLevel = 95 },
        };
    }
}
