using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Arayüzde gösterilecek subdomain satırı.</summary>
public sealed record SubdomainRow(string FullName, string IpAddress);

/// <summary>
/// Subdomain Scanner aracının sunum katmanı. İş mantığı <see cref="ISubdomainService"/> içindedir.
/// Adım sayısı ve eşzamanlı sorgu sayısı servis tarafından sınırlanır; araç kullanıcıya bu sınırı
/// açıkça gösterir, ilerleme bildirir ve iptal edilebilir.
/// </summary>
public partial class SubdomainViewModel : ObservableObject
{
    private readonly ISubdomainService _subdomainService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SubdomainViewModel> _logger;

    [ObservableProperty] private string _targetDomain = string.Empty;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private int _foundCount;
    [ObservableProperty] private bool _hasResults;

    /// <summary>Hedef alanı boş/geçersiz olduğunda görünüm oraya odaklanır.</summary>
    public event Action? RequestTargetFocus;

    public SubdomainViewModel(
        ISubdomainService subdomainService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<SubdomainViewModel> logger)
    {
        _subdomainService = subdomainService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Subdomain.Status.Ready");
        ProgressText = _localizer.Format("L.Subdomain.Progress.Initial", _subdomainService.MaxCandidatesPerScan);
    }

    public ObservableCollection<SubdomainRow> Subdomains { get; } = new();

    public string LimitsText => _localizer.Format(
        "L.Subdomain.Limits",
        _subdomainService.MaxCandidatesPerScan,
        _subdomainService.MaxConcurrency,
        _subdomainService.QueryTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ScanAsync(CancellationToken cancellationToken)
    {
        string target = TargetDomain?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Subdomain.Validation.InvalidDomain");
            return;
        }

        IsScanning = true;
        Subdomains.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        FoundCount = 0;
        ProgressPercent = 0;
        ProgressText = _localizer.Format("L.Subdomain.Progress.Initial", _subdomainService.MaxCandidatesPerScan);

        var progress = new Progress<SubdomainScanProgress>(OnProgress);

        try
        {
            SetStatus(NotificationType.Info, "L.Subdomain.Status.Scanning", target);

            ToolResult<SubdomainScanReport> result = await _subdomainService.ScanAsync(
                target, progress, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "subdomain-scan");
                return;
            }

            SubdomainScanReport report = result.Value!;

            foreach (SubdomainEntry entry in report.Found)
                Subdomains.Add(new SubdomainRow(entry.FullName, entry.IpAddress));

            FoundCount = report.FoundCount;
            HasResults = FoundCount > 0;
            ProgressPercent = 100;
            ProgressText = _localizer.Format("L.Subdomain.Progress.Scanning",
                report.CandidatesChecked, report.CandidatesChecked, report.FoundCount);
            SummaryText = _localizer.Format("L.Subdomain.Status.Summary",
                report.Domain,
                report.CandidatesChecked,
                report.FoundCount,
                report.FailedCount,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "Subdomain scan finished: domain={Domain} checked={Checked} found={Found} failed={Failed} elapsedMs={Elapsed}",
                report.Domain, report.CandidatesChecked, report.FoundCount, report.FailedCount,
                report.Elapsed.TotalMilliseconds);

            if (report.FoundCount == 0)
            {
                SetStatus(NotificationType.Info, "L.Subdomain.Status.NoResults", report.CandidatesChecked);
                return;
            }

            SetStatus(NotificationType.Success, "L.Subdomain.Status.Finished",
                report.FoundCount, report.CandidatesChecked);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Subdomain scan cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            ProgressText = _localizer.Get("L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subdomain scan failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void OnProgress(SubdomainScanProgress progress)
    {
        ProgressPercent = progress.Total > 0 ? progress.Checked * 100.0 / progress.Total : 0;
        ProgressText = _localizer.Format("L.Subdomain.Progress.Scanning",
            progress.Checked, progress.Total, progress.Found);
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Subdomains.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Subdomain.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.Subdomain.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subdomain result could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (Subdomains.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Subdomain.Validation.NothingToSave");
            return;
        }

        IsScanning = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt|{_localizer.Get("L.Subdomain.Filter.Csv")}|*.csv",
                ".txt",
                "subdomains");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Subdomain.Status.Saved" : "L.Subdomain.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subdomain result could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        Subdomains.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        FoundCount = 0;
        ProgressPercent = 0;
        ProgressText = _localizer.Format("L.Subdomain.Progress.Initial", _subdomainService.MaxCandidatesPerScan);
        SetStatus(NotificationType.Info, "L.Subdomain.Status.Cleared");
    }

    private string BuildReport()
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"{_localizer.Get("L.Subdomain.Column.Host")}\t{_localizer.Get("L.Subdomain.Column.Ip")}");

        foreach (SubdomainRow row in Subdomains)
            builder.AppendLine($"{row.FullName}\t{row.IpAddress}");

        return builder.ToString().TrimEnd();
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ScanCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
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
            _logger.LogInformation("Subdomain scan {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Subdomain scan {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}