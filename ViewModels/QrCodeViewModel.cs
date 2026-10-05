using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class QrCodeViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private BitmapImage? _qrCodeImage;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public QrCodeViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text or URL to generate QR code";
            QrCodeImage = null;
            return;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(InputText, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);

            using var stream = new MemoryStream(qrCodeBytes);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            QrCodeImage = bitmapImage;
            StatusText = "QR code generated successfully";
        }
        catch (System.Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            QrCodeImage = null;
        }
    }

    [RelayCommand]
    private void SaveAsImage()
    {
        if (QrCodeImage == null)
        {
            StatusText = "No QR code to save. Generate one first.";
            return;
        }

        var savePath = _fileExportService.RequestSavePath("PNG Image|*.png", ".png", "QRCode");
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(InputText, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                File.WriteAllBytes(savePath, qrCodeBytes);
                StatusText = $"QR code saved to {Path.GetFileName(savePath)}";
            }
            catch (System.Exception ex)
            {
                StatusText = $"Error saving: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        QrCodeImage = null;
        StatusText = "Cleared. Ready.";
    }
}