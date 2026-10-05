using System.Threading.Tasks;

namespace tool_cantam.Services.Interfaces
{
    /// <summary>
    /// Dosya dışarı aktarma ve diyalog işlemleri için merkezi servis arayüzü.
    /// Tüm ViewModel'ler dosya kaydetme, açma ve pano işlemleri için bu servisi kullanır.
    /// </summary>
    public interface IFileExportService
    {
        /// <summary>
        /// Kullanıcıdan SaveFileDialog ile kaydetme yolu alır.
        /// </summary>
        /// <returns>Seçilen yol veya iptal edilmişse null.</returns>
        string? RequestSavePath(string filter, string defaultExt, string defaultFileName = "");

        /// <summary>
        /// Kullanıcıdan OpenFileDialog ile dosya açma yolu alır.
        /// </summary>
        /// <param name="filter">Dosya filtresi, örn: "All Files (*.*)|*.*"</param>
        /// <param name="title">Diyalog başlığı</param>
        /// <returns>Seçilen dosya yolu veya iptal edilmişse null.</returns>
        string? RequestOpenPath(string filter, string title = "Select File");

        /// <summary>
        /// Verilen metni panoya kopyalar.
        /// </summary>
        void CopyToClipboard(string content);

        /// <summary>
        /// SaveFileDialog açar ve verilen metni seçilen dosyaya kaydeder.
        /// </summary>
        /// <returns>Başarılı ise true, aksi halde false.</returns>
        Task<bool> ExportTextAsync(string content, string filter, string defaultExt, string defaultFileName = "Export");
    }
}
