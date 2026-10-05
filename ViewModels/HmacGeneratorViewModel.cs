using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Security.Cryptography;
using System.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HmacGeneratorViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _secretKey = string.Empty;

    [ObservableProperty]
    private string _outputHash = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public HmacGeneratorViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to hash";
            OutputHash = string.Empty;
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            StatusText = "Please enter a secret key";
            OutputHash = string.Empty;
            return;
        }

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            var messageBytes = Encoding.UTF8.GetBytes(InputText);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            OutputHash = Convert.ToHexString(hashBytes).ToLower();
            StatusText = "HMAC-SHA256 generated successfully";
        }
        catch (Exception ex)
        {
            OutputHash = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (!string.IsNullOrWhiteSpace(OutputHash))
        {
            _fileExportService.CopyToClipboard(OutputHash);
            StatusText = "Hash copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        SecretKey = string.Empty;
        OutputHash = string.Empty;
        StatusText = "Cleared. Ready.";
    }
}