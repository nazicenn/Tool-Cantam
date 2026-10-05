using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class ReverseDnsViewModel : ObservableObject
{
    private readonly IReverseDnsService _reverseDnsService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetIp = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private string _result = string.Empty;

    public ReverseDnsViewModel(IReverseDnsService reverseDnsService, IFileExportService fileExportService)
    {
        _reverseDnsService = reverseDnsService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task LookupAsync()
    {
        TargetIp = TargetIp.Trim();

        if (string.IsNullOrWhiteSpace(TargetIp))
        {
            StatusText = "Please enter an IP address";
            return;
        }

        if (!_reverseDnsService.IsValidIp(TargetIp))
        {
            StatusText = "Invalid IP address format";
            return;
        }

        IsLoading = true;
        StatusText = "Looking up hostname...";
        Result = string.Empty;

        try
        {
            Result = await _reverseDnsService.LookupAsync(TargetIp);
            StatusText = "Reverse DNS lookup completed";
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
        if (!string.IsNullOrWhiteSpace(Result))
        {
            _fileExportService.CopyToClipboard(Result);
            StatusText = "Result copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TargetIp = string.Empty;
        Result = string.Empty;
        StatusText = "Cleared. Ready.";
    }
}