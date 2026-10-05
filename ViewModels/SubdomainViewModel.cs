using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class SubdomainViewModel : ObservableObject
{
    private readonly ISubdomainService _subdomainService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetDomain = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private int _foundCount;

    [ObservableProperty]
    private ObservableCollection<string> _subdomains = new();

    public SubdomainViewModel(ISubdomainService subdomainService, IFileExportService fileExportService)
    {
        _subdomainService = subdomainService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetDomain))
        {
            StatusText = "Please enter a domain (example: google.com)";
            return;
        }

        if (!_subdomainService.IsValidDomain(TargetDomain))
        {
            StatusText = "Invalid domain format. Use example: google.com";
            return;
        }

        IsScanning = true;
        Subdomains.Clear();
        StatusText = "Scanning subdomains...";
        FoundCount = 0;

        try
        {
            var results = await _subdomainService.ScanAsync(TargetDomain);

            foreach (var sub in results)
            {
                Subdomains.Add(sub);
            }

            FoundCount = results.Count;
            StatusText = FoundCount > 0
                ? $"Found {FoundCount} subdomains"
                : "No subdomains found";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Subdomains.Count == 0) return;

        var results = string.Join(Environment.NewLine, Subdomains);
        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetDomain = string.Empty;
        Subdomains.Clear();
        FoundCount = 0;
        StatusText = "Ready.";
    }
}