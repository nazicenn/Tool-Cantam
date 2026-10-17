using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public sealed record DnsRow(string Type, string Value, string PriorityDisplay);

public partial class DnsViewModel : ObservableObject
{
    private readonly IDnsService _dnsService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<DnsViewModel> _logger;

    [ObservableProperty] private string _targetDomain = string.Empty;
    [ObservableProperty] private bool _isQuerying;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    public event Action? RequestTargetFocus;

    public DnsViewModel(
        IDnsService dnsService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<DnsViewModel> logger)
    {
        _dnsService = dnsService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Dns.Status.Ready");
    }

    public ObservableCollection<DnsRow> Records { get; } = new();

    public string LimitsText => _localizer.Format(
        "L.Dns.Limits",
        _dnsService.QueryTimeoutMs,
        string.Join(", ", _dnsService.SupportedRecordTypes));

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task QueryAsync(CancellationToken cancellationToken)
    {
        string target = TargetDomain?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Dns.Validation.InvalidDomain");
            return;
        }

        IsQuerying = true;
        Records.Clear();
        HasResults = false;
        SummaryText = string.Empty;

        try
        {
            SetStatus(NotificationType.Info, "L.Dns.Status.Querying", target);

            ToolResult<DnsLookupReport> result = await _dnsService.LookupAsync(target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "dns-lookup");
                return;
            }

            DnsLookupReport report = result.Value!;

            foreach (var entry in report.Records)
            {
                string priority = entry.Priority.HasValue ? entry.Priority.Value.ToString() : "—";
                Records.Add(new DnsRow(entry.Type, entry.Value, priority));
            }

            HasResults = Records.Count > 0;
            SummaryText = _localizer.Format("L.Dns.Status.Summary",
                report.Domain,
                report.TotalCount,
                report.CountByType("A"),
                report.CountByType("AAAA"),
                report.CountByType("MX"),
                report.CountByType("NS"),
                report.CountByType("TXT"),
                report.CountByType("SOA"),
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "DNS lookup finished: domain={Domain} total={Total} elapsedMs={Elapsed}",
                report.Domain, report.TotalCount, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.Dns.Status.Finished", report.TotalCount, report.Domain);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DNS lookup cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS lookup failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsQuerying = false;
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (Records.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Dns.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.Dns.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (Records.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Dns.Validation.NothingToSave");
            return;
        }

        IsQuerying = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt|{_localizer.Get("L.Dns.Filter.Csv")}|*.csv",
                ".txt",
                "dns-lookup");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Dns.Status.Saved" : "L.Dns.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNS result could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsQuerying = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        if (QueryCancelCommand is not null && QueryCancelCommand.CanExecute(null))
            QueryCancelCommand.Execute(null);

        Records.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.Dns.Status.Cleared");
    }

    private string BuildReport()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"{_localizer.Get("L.Dns.Column.Type")}\t{_localizer.Get("L.Dns.Column.Priority")}\t{_localizer.Get("L.Dns.Column.Value")}");

        foreach (var row in Records)
            builder.AppendLine($"{row.Type}\t{row.PriorityDisplay}\t{row.Value}");

        return builder.ToString().TrimEnd();
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
            _logger.LogInformation("DNS lookup {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("DNS lookup {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}