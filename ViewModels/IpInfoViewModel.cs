using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// IP Info aracının sunum katmanı. İş mantığı <see cref="IIpInfoService"/> içindedir.
/// Sorgu async, iptal edilebilir; sonuç coğrafi ve ağ bilgisi olarak gösterilir.
/// </summary>
public partial class IpInfoViewModel : ObservableObject
{
    private readonly IIpInfoService _ipInfoService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<IpInfoViewModel> _logger;

    [ObservableProperty] private string _targetIp = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    // Coğrafi
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _region = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _zip = string.Empty;
    [ObservableProperty] private string _coordinates = string.Empty;
    [ObservableProperty] private string _timezone = string.Empty;

    // Ağ
    [ObservableProperty] private string _isp = string.Empty;
    [ObservableProperty] private string _org = string.Empty;
    [ObservableProperty] private string _asn = string.Empty;
    [ObservableProperty] private string _addressFamily = string.Empty;

    public event Action? RequestTargetFocus;

    public IpInfoViewModel(
        IIpInfoService ipInfoService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<IpInfoViewModel> logger)
    {
        _ipInfoService = ipInfoService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.IpInfo.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.IpInfo.Limits", _ipInfoService.QueryTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LookupAsync(CancellationToken cancellationToken)
    {
        string target = TargetIp?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.IpInfo.Validation.InvalidIp");
            return;
        }

        IsLoading = true;
        ClearResults();
        HasResults = false;

        try
        {
            SetStatus(NotificationType.Info, "L.IpInfo.Status.Querying", target);

            ToolResult<IpInfoReport> result = await _ipInfoService.GetIpInfoAsync(
                target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "ip-info");
                return;
            }

            IpInfoReport report = result.Value!;

            Country = report.Country;
            Region = report.Region;
            City = report.City;
            Zip = report.Zip;
            Coordinates = report.Coordinates;
            Timezone = report.Timezone;
            Isp = report.Isp;
            Org = report.Org;
            Asn = report.Asn;
            AddressFamily = report.AddressFamily;
            HasResults = true;

            SummaryText = _localizer.Format("L.IpInfo.Status.Summary",
                report.IpAddress,
                report.Location,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "IP info finished: ip={Ip} country={Country} city={City} isp={Isp} elapsedMs={Elapsed}",
                report.IpAddress, report.Country, report.City, report.Isp, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.IpInfo.Status.Finished", report.Location);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("IP info lookup cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP info lookup failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.IpInfo.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.IpInfo.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP info result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.IpInfo.Validation.NothingToSave");
            return;
        }

        IsLoading = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "ip-info");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.IpInfo.Status.Saved" : "L.IpInfo.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP info result could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        if (LookupCancelCommand is not null && LookupCancelCommand.CanExecute(null))
            LookupCancelCommand.Execute(null);

        TargetIp = string.Empty;
        ClearResults();
        HasResults = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.IpInfo.Status.Cleared");
    }

    private void ClearResults()
    {
        Country = string.Empty;
        Region = string.Empty;
        City = string.Empty;
        Zip = string.Empty;
        Coordinates = string.Empty;
        Timezone = string.Empty;
        Isp = string.Empty;
        Org = string.Empty;
        Asn = string.Empty;
        AddressFamily = string.Empty;
    }

    private string BuildReport()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Ip")}: {TargetIp}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Family")}: {AddressFamily}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Country")}: {Country}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Region")}: {Region}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.City")}: {City}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Zip")}: {Zip}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Coordinates")}: {Coordinates}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Timezone")}: {Timezone}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Isp")}: {Isp}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Org")}: {Org}");
        b.AppendLine($"{_localizer.Get("L.IpInfo.Column.Asn")}: {Asn}");
        return b.ToString().TrimEnd();
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("IP info {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("IP info {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}