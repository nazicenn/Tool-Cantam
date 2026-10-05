using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class SslCheckerViewModel : ObservableObject
{
    private readonly ISslCheckerService _sslCheckerService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetDomain = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private SslCertificateInfo? _certificateInfo;

    public SslCheckerViewModel(ISslCheckerService sslCheckerService, IFileExportService fileExportService)
    {
        _sslCheckerService = sslCheckerService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetDomain))
        {
            StatusText = "Please enter a domain name";
            return;
        }

        if (!_sslCheckerService.IsValidDomain(TargetDomain))
        {
            StatusText = "Invalid domain format";
            return;
        }

        IsLoading = true;
        StatusText = "Checking SSL certificate...";
        CertificateInfo = null;

        try
        {
            var result = await _sslCheckerService.CheckAsync(TargetDomain);
            CertificateInfo = result;
            StatusText = result.IsValid ? "Certificate is valid" : $"Certificate invalid: {result.Status}";
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
        if (CertificateInfo == null) return;

        var results = $"SSL Certificate Info for {CertificateInfo.Domain}\n" +
                      $"Subject: {CertificateInfo.Subject}\n" +
                      $"Issuer: {CertificateInfo.Issuer}\n" +
                      $"Valid From: {CertificateInfo.NotBefore:yyyy-MM-dd}\n" +
                      $"Valid To: {CertificateInfo.NotAfter:yyyy-MM-dd}\n" +
                      $"Remaining Days: {CertificateInfo.RemainingDays}\n" +
                      $"Status: {CertificateInfo.Status}\n" +
                      $"Serial Number: {CertificateInfo.SerialNumber}\n" +
                      $"Algorithm: {CertificateInfo.SignatureAlgorithm}";

        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetDomain = string.Empty;
        CertificateInfo = null;
        StatusText = "Ready.";
    }
}