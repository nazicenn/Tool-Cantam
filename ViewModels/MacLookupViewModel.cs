using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class MacLookupViewModel : ObservableObject
{
    private readonly IMacLookupService _macLookupService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _macAddress = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private string _vendor = string.Empty;

    [ObservableProperty]
    private string _formattedMac = string.Empty;

    public MacLookupViewModel(IMacLookupService macLookupService, IFileExportService fileExportService)
    {
        _macLookupService = macLookupService;
        _fileExportService = fileExportService;
    }

    partial void OnMacAddressChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var clean = value.Trim().Replace("-", "").Replace(":", "").ToUpper();
            if (clean.Length >= 6)
            {
                var formatted = "";
                for (int i = 0; i < clean.Length && i < 12; i += 2)
                {
                    if (i + 2 <= clean.Length)
                        formatted += clean.Substring(i, 2) + (i + 2 < 12 ? ":" : "");
                }
                FormattedMac = formatted;
            }
            else
            {
                FormattedMac = string.Empty;
            }
        }
        else
        {
            FormattedMac = string.Empty;
        }
    }

    [RelayCommand]
    private async Task LookupAsync()
    {
        if (string.IsNullOrWhiteSpace(MacAddress))
        {
            StatusText = "Please enter a MAC address";
            return;
        }

        if (!_macLookupService.IsValidMac(MacAddress))
        {
            StatusText = "Invalid MAC address format (e.g., 00:1A:2B:3C:4D:5E)";
            return;
        }

        IsLoading = true;
        StatusText = "Looking up vendor...";
        Vendor = string.Empty;

        try
        {
            var result = await _macLookupService.GetVendorAsync(MacAddress);
            Vendor = result;
            StatusText = "MAC lookup completed";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrWhiteSpace(Vendor))
        {
            _fileExportService.CopyToClipboard(Vendor);
            StatusText = "Result copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        MacAddress = string.Empty;
        Vendor = string.Empty;
        FormattedMac = string.Empty;
        StatusText = "Cleared. Ready.";
    }
}