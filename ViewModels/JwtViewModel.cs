using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class JwtViewModel : ObservableObject
{
    private readonly IJwtService _jwtService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _jwtInput = string.Empty;

    [ObservableProperty]
    private string _decodedHeader = string.Empty;

    [ObservableProperty]
    private string _decodedPayload = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";  

    public JwtViewModel(IJwtService jwtService, IFileExportService fileExportService)
    {
        _jwtService = jwtService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void DecodeJwt()
    {
        if (string.IsNullOrWhiteSpace(JwtInput))
        {
            DecodedHeader = "JWT input is empty.";
            DecodedPayload = string.Empty;
            StatusText = "Please enter a JWT token";
            return;
        }

        try
        {
            DecodedHeader = _jwtService.DecodeHeader(JwtInput);
            DecodedPayload = _jwtService.DecodePayload(JwtInput);
            StatusText = "JWT decoded successfully";
        }
        catch (FormatException ex)
        {
            DecodedHeader = $"Invalid JWT format: {ex.Message}";
            DecodedPayload = string.Empty;
            StatusText = "Invalid JWT format";
        }
        catch (Exception ex)
        {
            DecodedHeader = $"Error: {ex.Message}";
            DecodedPayload = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyPayload()
    {
        if (!string.IsNullOrWhiteSpace(DecodedPayload))
        {
            _fileExportService.CopyToClipboard(DecodedPayload);
            StatusText = "Payload copied to clipboard";
        }
    }

    [RelayCommand]
    private async Task DownloadPayloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DecodedPayload)) return;
        await _fileExportService.ExportTextAsync(DecodedPayload, "JSON Files|*.json|All Files|*.*", ".json", "JwtPayload");
        StatusText = "Payload downloaded";
    }

    [RelayCommand]
    private void Clear()
    {
        JwtInput = string.Empty;
        DecodedHeader = string.Empty;
        DecodedPayload = string.Empty;
        StatusText = "Cleared. Ready.";
    }
}