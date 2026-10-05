// ViewModels/QrCodeReaderViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

namespace tool_cantam.ViewModels;

public partial class QrCodeReaderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _qrText = string.Empty;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void ReadQrCode()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            StatusText = "Please select or drop a QR code image";
            return;
        }

        try
        {
            if (!File.Exists(FilePath))
            {
                StatusText = "File not found";
                return;
            }

            // Load preview
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(FilePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            PreviewImage = bitmap;

            // Read QR code
            var reader = new BarcodeReaderGeneric();
            using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            var img = new System.Drawing.Bitmap(fs);
            var result = reader.Decode(img);

            if (result != null)
            {
                QrText = result.Text;
                StatusText = $"✓ QR Code detected: {result.BarcodeFormat}";
            }
            else
            {
                QrText = "No QR code found in this image";
                StatusText = "✗ No QR code detected";
            }
        }
        catch (Exception ex)
        {
            QrText = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrWhiteSpace(QrText) && QrText != "No QR code found in this image")
        {
            Clipboard.SetText(QrText);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        FilePath = string.Empty;
        QrText = string.Empty;
        PreviewImage = null;
        StatusText = "Cleared";
    }
}