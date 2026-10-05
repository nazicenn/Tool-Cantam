using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services
{
    /// <summary>
    /// Dosya dışarı aktarma servis uygulaması.
    /// SaveFileDialog, OpenFileDialog ve Clipboard işlemlerini ViewModel'lerden izole eder.
    /// </summary>
    public class FileExportService : IFileExportService
    {
        public string? RequestSavePath(string filter, string defaultExt, string defaultFileName = "")
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        public string? RequestOpenPath(string filter, string title = "Select File")
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        public void CopyToClipboard(string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            Clipboard.SetText(content);
        }

        public async Task<bool> ExportTextAsync(string content, string filter, string defaultExt, string defaultFileName = "Export")
        {
            var path = RequestSavePath(filter, defaultExt, defaultFileName);
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                await File.WriteAllTextAsync(path, content ?? string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileExportService] Dosya yazma hatasi: {ex.Message}");
                return false;
            }
        }
    }
}

