using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class GeoIpLookupViewModel : ObservableObject
{
    private readonly IGeoIpLookupService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<GeoIpLookupViewModel> _logger;

    [ObservableProperty] private string _ipAddress = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _countryCode = string.Empty;
    [ObservableProperty] private string _region = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _zipCode = string.Empty;
    [ObservableProperty] private string _latitude = string.Empty;
    [ObservableProperty] private string _longitude = string.Empty;
    [ObservableProperty] private string _isp = string.Empty;
    [ObservableProperty] private string _organization = string.Empty;
    [ObservableProperty] private string _timezone = string.Empty;
    [ObservableProperty] private bool _isProxy;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public GeoIpLookupViewModel(IGeoIpLookupService service, IFileExportService fe, ILocalizerService loc, ILogger<GeoIpLookupViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.GeoIpLookup.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.GeoIpLookup.Limits", _service.RequestTimeout.TotalSeconds);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLookup))]
    private async Task LookupAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.LookupAsync(IpAddress, ct);
            if (!r.IsSuccess) { ReportFailure(r, "lookup"); ClearResults(); return; }
            var v = r.Value!;
            Country = v.Country; CountryCode = v.CountryCode; Region = v.Region;
            City = v.City; ZipCode = v.Zip;
            Latitude = v.Lat.ToString("F4"); Longitude = v.Lon.ToString("F4");
            Isp = v.Isp; Organization = v.Org; Timezone = v.Timezone;
            IsProxy = v.IsProxy;
            _logger.LogInformation("GeoIP: {Ip} → {Country}, {City}", v.IpAddress, v.Country, v.City);
            SetStatus(NotificationType.Success, "L.GeoIpLookup.Status.Found", v.City, v.Country);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "GeoIP failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanLookup() => !string.IsNullOrWhiteSpace(IpAddress);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LookupMyIpAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.GetMyIpAsync(ct);
            if (!r.IsSuccess) { ReportFailure(r, "myip"); return; }
            IpAddress = r.Value!;
            await LookupAsync(ct);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (string.IsNullOrWhiteSpace(Country)) { SetStatus(NotificationType.Warning, "L.GeoIpLookup.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder();
        sb.AppendLine($"IP: {IpAddress}");
        sb.AppendLine($"Country: {Country} ({CountryCode})");
        sb.AppendLine($"Region: {Region}");
        sb.AppendLine($"City: {City}");
        sb.AppendLine($"ZIP: {ZipCode}");
        sb.AppendLine($"Coordinates: {Latitude}, {Longitude}");
        sb.AppendLine($"ISP: {Isp}");
        sb.AppendLine($"Org: {Organization}");
        sb.AppendLine($"Timezone: {Timezone}");
        sb.AppendLine($"Proxy: {IsProxy}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.GeoIpLookup.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand] private void Clear() { IpAddress = string.Empty; ClearResults(); SetStatus(NotificationType.Info, "L.GeoIpLookup.Status.Cleared"); }

    private void ClearResults() { Country = string.Empty; CountryCode = string.Empty; Region = string.Empty; City = string.Empty; ZipCode = string.Empty; Latitude = string.Empty; Longitude = string.Empty; Isp = string.Empty; Organization = string.Empty; Timezone = string.Empty; IsProxy = false; }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("GeoIpLookup {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}