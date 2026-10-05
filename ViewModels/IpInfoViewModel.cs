using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class IpInfoViewModel : ObservableObject
{
    private readonly IIpInfoService _ipInfoService;
    private readonly IFileExportService _fileExportService;

    private string _targetIp = string.Empty;
    public string TargetIp
    {
        get => _targetIp;
        set => SetProperty(ref _targetIp, value?.Trim() ?? string.Empty);
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private IpInfoModel? _ipInfo;

    public IpInfoViewModel(IIpInfoService ipInfoService, IFileExportService fileExportService)
    {
        _ipInfoService = ipInfoService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task LookupAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetIp))
        {
            StatusText = "Please enter an IP address";
            return;
        }

        if (!_ipInfoService.IsValidIp(TargetIp))
        {
            StatusText = "Invalid IP address format";
            return;
        }

        IsLoading = true;
        StatusText = "Looking up IP information...";
        IpInfo = null;

        try
        {
            var result = await _ipInfoService.GetIpInfoAsync(TargetIp);
            IpInfo = result;
            StatusText = result?.Status == "success" ? "IP lookup completed" : "IP not found";
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
        if (IpInfo == null || IpInfo.Status != "success") return;

        var results = $"IP: {IpInfo.Query}\nCountry: {IpInfo.Country}\nCity: {IpInfo.City}\nRegion: {IpInfo.RegionName}\nISP: {IpInfo.Isp}\nOrganization: {IpInfo.Org}\nTimezone: {IpInfo.Timezone}";
        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetIp = string.Empty;
        IpInfo = null;
        StatusText = "Cleared. Ready.";
    }
}