using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// MAC Lookup aracının sunum katmanı. İş mantığı <see cref="IMacLookupService"/> içindedir.
/// Sorgu async, iptal edilebilir; sonuç satıcı + adres + tip olarak gösterilir.
/// </summary>
public partial class MacLookupViewModel : ObservableObject
{
    private readonly IMacLookupService _macLookupService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<MacLookupViewModel> _logger;

    [ObservableProperty] private string _macAddress = string.Empty;
    [ObservableProperty] private string _formattedMac = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    [ObservableProperty] private string _vendor = string.Empty;
    [ObservableProperty] private string _oui = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private string _country = string.Empty;
    [ObservableProperty] private string _macType = string.Empty;

    public event Action? RequestTargetFocus;

    public MacLookupViewModel(
        IMacLookupService macLookupService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<MacLookupViewModel> logger)
    {
        _macLookupService = macLookupService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.MacLookup.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.MacLookup.Limits", _macLookupService.QueryTimeoutMs);

    partial void OnMacAddressChanged(string value)
    {
        // Live preview: formatla ama doğrulama yapma
        if (string.IsNullOrWhiteSpace(value))
        {
            FormattedMac = string.Empty;
            return;
        }

        string clean = value.Trim()
            .Replace(":", "")
            .Replace("-", "")
            .Replace(".", "")
            .Replace(" ", "")
            .ToUpperInvariant();

        if (clean.Length == 12)
        {
            FormattedMac = string.Join(":",
                clean[..2], clean[2..4], clean[4..6],
                clean[6..8], clean[8..10], clean[10..12]);
        }
        else
        {
            FormattedMac = string.Empty;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LookupAsync(CancellationToken cancellationToken)
    {
        string target = MacAddress?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.MacLookup.Validation.InvalidMac");
            return;
        }

        IsLoading = true;
        ClearResults();
        HasResults = false;

        try
        {
            SetStatus(NotificationType.Info, "L.MacLookup.Status.Querying", target);

            ToolResult<MacLookupReport> result = await _macLookupService.LookupVendorAsync(
                target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "mac-lookup");
                return;
            }

            MacLookupReport report = result.Value!;

            Oui = report.Oui;
            Vendor = report.Vendor;
            Address = report.Address;
            Country = report.Country;
            MacType = report.MacType;
            FormattedMac = report.FormattedMac;
            HasResults = true;

            SummaryText = _localizer.Format("L.MacLookup.Status.Summary",
                report.FormattedMac,
                report.MacType,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "MAC lookup finished: mac={Mac} oui={Oui} vendor={Vendor} elapsedMs={Elapsed}",
                report.FormattedMac, report.Oui, report.Vendor, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.MacLookup.Status.Finished", report.Vendor);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MAC lookup cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MAC lookup failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.MacLookup.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.MacLookup.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MAC lookup result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.MacLookup.Validation.NothingToSave");
            return;
        }

        IsLoading = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "mac-lookup");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.MacLookup.Status.Saved" : "L.MacLookup.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MAC lookup result could not be saved.");
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

        MacAddress = string.Empty;
        FormattedMac = string.Empty;
        ClearResults();
        HasResults = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.MacLookup.Status.Cleared");
    }

    private void ClearResults()
    {
        Vendor = string.Empty;
        Oui = string.Empty;
        Address = string.Empty;
        Country = string.Empty;
        MacType = string.Empty;
    }

    private string BuildReport()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Mac")}: {FormattedMac}");
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Oui")}: {Oui}");
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Vendor")}: {Vendor}");
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Address")}: {Address}");
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Country")}: {Country}");
        b.AppendLine($"{_localizer.Get("L.MacLookup.Column.Type")}: {MacType}");
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
            _logger.LogInformation("MAC lookup {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("MAC lookup {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}