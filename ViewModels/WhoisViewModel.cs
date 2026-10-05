using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class WhoisViewModel : ObservableObject
{
    private readonly IWhoisService _whoisService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetDomain = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private WhoisModel? _whoisInfo;

    public WhoisViewModel(IWhoisService whoisService, IFileExportService fileExportService)
    {
        _whoisService = whoisService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task LookupAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetDomain))
        {
            StatusText = "Please enter a domain name";
            return;
        }

        IsLoading = true;
        StatusText = "Looking up WHOIS information...";
        WhoisInfo = null;

        try
        {
            var result = await _whoisService.LookupAsync(TargetDomain);
            WhoisInfo = result;
            StatusText = result.Success ? "WHOIS lookup completed" : $"Error: {result.ErrorMessage}";
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
        if (WhoisInfo == null) return;

        var results = $"Domain: {WhoisInfo.Domain}\n" +
                      $"Registrar: {WhoisInfo.Registrar}\n" +
                      $"Creation Date: {WhoisInfo.CreationDate}\n" +
                      $"Expiration Date: {WhoisInfo.ExpirationDate}\n" +
                      $"Name Servers: {WhoisInfo.NameServers}\n" +
                      $"Status: {WhoisInfo.Status}\n" +
                      $"Owner: {WhoisInfo.Owner}\n" +
                      $"Country: {WhoisInfo.OwnerCountry}";

        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetDomain = string.Empty;
        WhoisInfo = null;
        StatusText = "Ready.";
    }
}