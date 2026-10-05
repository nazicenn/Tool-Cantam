using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class Base64ImageConverterViewModel : ObservableObject
{
    private readonly IImageConverterService _imageConverterService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private string _base64Text = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private BitmapImage? _previewImage;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public Base64ImageConverterViewModel(
        IImageConverterService imageConverterService,
        IClipboardService clipboardService)
    {
        _imageConverterService = imageConverterService;
        _clipboardService = clipboardService;
    }

    partial void OnFilePathChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
        {
            ConvertImageToBase64();
        }
    }

    [RelayCommand]
    private void ConvertImageToBase64()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            UpdateStatus("Please select or drop an image file", NotificationType.Warning);
            return;
        }

        if (!File.Exists(FilePath))
        {
            UpdateStatus($"File not found: {FilePath}", NotificationType.Error);
            return;
        }

        try
        {
            // Secure conversion (only allows image files)
            Base64Text = _imageConverterService.ConvertImageToBase64(FilePath);

            // Load preview
            PreviewImage = new BitmapImage(new Uri(FilePath));

            UpdateStatus($"✓ Converted: {Path.GetFileName(FilePath)}", NotificationType.Success);
        }
        catch (InvalidOperationException ex)
        {
            UpdateStatus(ex.Message, NotificationType.Error);
            PreviewImage = null;
        }
        catch (Exception ex)
        {
            UpdateStatus($"✗ Conversion failed: {ex.Message}", NotificationType.Error);
            PreviewImage = null;
        }
    }

    [RelayCommand]
    private void ConvertBase64ToImage()
    {
        if (string.IsNullOrWhiteSpace(Base64Text))
        {
            UpdateStatus("Please enter Base64 text", NotificationType.Warning);
            PreviewImage = null;
            return;
        }

        try
        {
            if (_imageConverterService.TryConvertBase64ToImage(Base64Text, out byte[] imageBytes, out string errorMessage))
            {
                // Load preview
                using var ms = new MemoryStream(imageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage = bitmap;

                UpdateStatus("✓ Base64 converted to image", NotificationType.Success);
            }
            else
            {
                PreviewImage = null;
                UpdateStatus($"✗ {errorMessage}", NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            PreviewImage = null;
            UpdateStatus($"✗ Unexpected error: {ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void CopyBase64()
    {
        if (string.IsNullOrWhiteSpace(Base64Text))
        {
            UpdateStatus("Nothing to copy - Base64 is empty", NotificationType.Warning);
            return;
        }

        try
        {
            _clipboardService.CopyToClipboard(Base64Text);
            UpdateStatus("✓ Base64 copied to clipboard!", NotificationType.Success);
        }
        catch (Exception ex)
        {
            UpdateStatus($"✗ Copy failed: {ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Base64Text = string.Empty;
        FilePath = string.Empty;
        PreviewImage = null;
        UpdateStatus("✓ Cleared", NotificationType.Success);
    }

    private void UpdateStatus(string message, NotificationType type)
    {
        StatusText = message;
        StatusType = type;

        if (type == NotificationType.Success)
        {
            System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
            {
                if (StatusText == message)
                {
                    StatusText = "Ready";
                    StatusType = NotificationType.Info;
                }
            });
        }
    }
}