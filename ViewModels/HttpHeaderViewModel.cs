using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using tool_cantam.Core.Enums;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HttpHeaderViewModel : ObservableObject
{
    private readonly IHttpHeaderService _httpHeaderService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetUrl = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private ObservableCollection<HeaderItem> _headers = new();

    public HttpHeaderViewModel(IHttpHeaderService httpHeaderService, IFileExportService fileExportService)
    {
        _httpHeaderService = httpHeaderService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetUrl))
        {
            StatusText = "Please enter a URL";
            return;
        }

        if (!_httpHeaderService.IsValidUrl(TargetUrl))
        {
            StatusText = "Invalid URL format. Use: https://example.com";
            return;
        }

        IsLoading = true;
        StatusText = "Analyzing HTTP headers...";
        Headers.Clear();

        try
        {
            var results = await _httpHeaderService.GetHeadersAsync(TargetUrl);

            foreach (var item in results)
            {
                Headers.Add(new HeaderItem { Key = item.Key, Value = item.Value });
            }

            StatusText = Headers.Count > 0
                ? $"Found {Headers.Count} headers"
                : "No headers found or request failed";
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
    private void CopyResults()
    {
        if (Headers.Count == 0) return;

        var results = $"HTTP Headers for {TargetUrl}\n";
        results += new string('=', 40) + "\n";

        foreach (var header in Headers)
        {
            results += $"{header.Key}: {header.Value}\n";
        }

        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetUrl = string.Empty;
        Headers.Clear();
        StatusText = "Cleared. Ready.";
    }
}

public class HeaderItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}