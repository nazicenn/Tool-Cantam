using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class ReverseDnsViewModel : ObservableObject
{
    private readonly IReverseDnsService _reverseDnsService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<ReverseDnsViewModel> _logger;

    [ObservableProperty] private string _targetIp = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _hostName = string.Empty;
    [ObservableProperty] private string _addressFamily = string.Empty;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _hasAliases;

    public ObservableCollection<string> Aliases { get; } = new();

    public event Action? RequestTargetFocus;

    public ReverseDnsViewModel(
        IReverseDnsService reverseDnsService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<ReverseDnsViewModel> logger)
    {
        _reverseDnsService = reverseDnsService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.ReverseDns.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.ReverseDns.Limits", _reverseDnsService.QueryTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LookupAsync(CancellationToken cancellationToken)
    {
        string target = TargetIp?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.ReverseDns.Validation.InvalidIp");
            return;
        }

        IsLoading = true;
        Aliases.Clear();
        HostName = string.Empty;
        AddressFamily = string.Empty;
        SummaryText = string.Empty;
        HasResults = false;
        HasAliases = false;

        try
        {
            SetStatus(NotificationType.Info, "L.ReverseDns.Status.Querying", target);

            ToolResult<ReverseDnsReport> result = await _reverseDnsService.LookupAsync(
                target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "reverse-dns");
                return;
            }

            ReverseDnsReport report = result.Value!;

            HostName = report.HostName;
            AddressFamily = report.AddressFamily;
            HasResults = true;
            HasAliases = report.Aliases.Count > 0;

            foreach (var alias in report.Aliases)
                Aliases.Add(alias);

            SummaryText = _localizer.Format("L.ReverseDns.Status.Summary",
                report.IpAddress,
                report.AddressFamily,
                report.Aliases.Count,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "Reverse DNS lookup finished: ip={Ip} family={Family} host={Host} aliases={AliasCount} elapsedMs={Elapsed}",
                report.IpAddress, report.AddressFamily, report.HostName,
                report.Aliases.Count, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.ReverseDns.Status.Finished", report.HostName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Reverse DNS lookup cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse DNS lookup failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.ReverseDns.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.ReverseDns.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse DNS result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.ReverseDns.Validation.NothingToSave");
            return;
        }

        IsLoading = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "reverse-dns");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.ReverseDns.Status.Saved" : "L.ReverseDns.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reverse DNS result could not be saved.");
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
        HostName = string.Empty;
        AddressFamily = string.Empty;
        Aliases.Clear();
        SummaryText = string.Empty;
        HasResults = false;
        HasAliases = false;
        SetStatus(NotificationType.Info, "L.ReverseDns.Status.Cleared");
    }

    private string BuildReport()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"{_localizer.Get("L.ReverseDns.Column.Ip")}: {TargetIp}");
        builder.AppendLine($"{_localizer.Get("L.ReverseDns.Column.Family")}: {AddressFamily}");
        builder.AppendLine($"{_localizer.Get("L.ReverseDns.Column.Hostname")}: {HostName}");

        if (Aliases.Count > 0)
        {
            builder.AppendLine($"{_localizer.Get("L.ReverseDns.Column.Aliases")}:");
            foreach (var alias in Aliases)
                builder.AppendLine($"  {alias}");
        }

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
            _logger.LogInformation("Reverse DNS {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Reverse DNS {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}