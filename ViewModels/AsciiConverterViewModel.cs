using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class AsciiConverterViewModel : ObservableObject
{
    private readonly IAsciiConverterService _asciiService;
    private readonly IClipboardService _clipboardService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public AsciiConverterViewModel(
        IAsciiConverterService asciiService,
        IClipboardService clipboardService,
        INotificationService notificationService)
    {
        _asciiService = asciiService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private void ToAscii()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            StatusType = NotificationType.Warning;
            OutputText = string.Empty;
            return;
        }

        try
        {
            OutputText = _asciiService.ConvertToAscii(InputText);
            StatusText = "✓ Converted to ASCII codes";
            StatusType = NotificationType.Success;
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
            StatusType = NotificationType.Error;
        }
    }

    [RelayCommand]
    private void FromAscii()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter ASCII codes (e.g., 65 66 67)";
            StatusType = NotificationType.Warning;
            OutputText = string.Empty;
            return;
        }

        try
        {
            if (_asciiService.TryParseAsciiCodes(InputText, out string result, out string errorMessage))
            {
                OutputText = result;
                StatusText = "✓ Converted from ASCII codes";
                StatusType = NotificationType.Success;
            }
            else
            {
                OutputText = string.Empty;
                StatusText = $"✗ Invalid format: {errorMessage}";
                StatusType = NotificationType.Error;
            }
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
            StatusType = NotificationType.Error;
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            StatusText = "Nothing to copy - output is empty";
            StatusType = NotificationType.Warning;
            return;
        }

        try
        {
            _clipboardService.CopyToClipboard(OutputText);
            StatusText = "✓ Output copied to clipboard";
            StatusType = NotificationType.Success;
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Failed to copy: {ex.Message}";
            StatusType = NotificationType.Error;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusText = "✓ Cleared. Ready.";
        StatusType = NotificationType.Success;
    }
}