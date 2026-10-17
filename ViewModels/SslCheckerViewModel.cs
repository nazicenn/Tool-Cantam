using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// SSL Checker aracının sunum katmanı. İş mantığı <see cref="ISslCheckerService"/> içindedir.
/// Sorgu async, iptal edilebilir; sonuç sertifika detayları + zincir hataları olarak gösterilir.
/// </summary>
public partial class SslCheckerViewModel : ObservableObject
{
    private readonly ISslCheckerService _sslCheckerService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SslCheckerViewModel> _logger;

    [ObservableProperty] private string _targetDomain = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    [ObservableProperty] private string _certificateStatus = string.Empty;
    [ObservableProperty] private bool _isValid;
    [ObservableProperty] private string _subject = string.Empty;
    [ObservableProperty] private string _issuer = string.Empty;
    [ObservableProperty] private string _commonName = string.Empty;
    [ObservableProperty] private string _validityText = string.Empty;
    [ObservableProperty] private int _remainingDays;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _signatureAlgorithm = string.Empty;
    [ObservableProperty] private string _tlsProtocol = string.Empty;

    public ObservableCollection<string> ChainIssues { get; } = new();

    public event Action? RequestTargetFocus;

    public SslCheckerViewModel(
        ISslCheckerService sslCheckerService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<SslCheckerViewModel> logger)
    {
        _sslCheckerService = sslCheckerService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.SslChecker.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.SslChecker.Limits", _sslCheckerService.QueryTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        string target = TargetDomain?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.SslChecker.Validation.InvalidDomain");
            return;
        }

        IsLoading = true;
        ClearResults();
        HasResults = false;

        try
        {
            SetStatus(NotificationType.Info, "L.SslChecker.Status.Checking", target);

            ToolResult<SslCertificateReport> result = await _sslCheckerService.CheckAsync(
                target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "ssl-check");
                return;
            }

            SslCertificateReport report = result.Value!;

            CertificateStatus = report.Status;
            IsValid = report.IsValid;
            Subject = report.Subject;
            Issuer = report.Issuer;
            CommonName = report.CommonName;
            ValidityText = report.ValidityText;
            RemainingDays = report.RemainingDays;
            SerialNumber = report.SerialNumber;
            SignatureAlgorithm = report.SignatureAlgorithm;
            TlsProtocol = report.TlsProtocol;
            HasResults = true;

            foreach (var issue in report.ChainIssues)
                ChainIssues.Add(issue);

            SummaryText = _localizer.Format("L.SslChecker.Status.Summary",
                report.Domain,
                report.Status,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "SSL check finished: domain={Domain} status={Status} remainingDays={Days} elapsedMs={Elapsed}",
                report.Domain, report.Status, report.RemainingDays, report.Elapsed.TotalMilliseconds);

            if (report.IsValid)
            {
                SetStatus(NotificationType.Success, "L.SslChecker.Status.Valid", report.RemainingDays);
            }
            else
            {
                SetStatus(NotificationType.Warning, "L.SslChecker.Status.Invalid", report.Status);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSL check cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSL check failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.SslChecker.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.SslChecker.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSL result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.SslChecker.Validation.NothingToSave");
            return;
        }

        IsLoading = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                $"{TargetDomain}-ssl");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.SslChecker.Status.Saved" : "L.SslChecker.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSL result could not be saved.");
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
        if (CheckCancelCommand is not null && CheckCancelCommand.CanExecute(null))
            CheckCancelCommand.Execute(null);

        TargetDomain = string.Empty;
        ClearResults();
        HasResults = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.SslChecker.Status.Cleared");
    }

    private void ClearResults()
    {
        CertificateStatus = string.Empty;
        IsValid = false;
        Subject = string.Empty;
        Issuer = string.Empty;
        CommonName = string.Empty;
        ValidityText = string.Empty;
        RemainingDays = 0;
        SerialNumber = string.Empty;
        SignatureAlgorithm = string.Empty;
        TlsProtocol = string.Empty;
        ChainIssues.Clear();
    }

    private string BuildReport()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.Domain")}: {TargetDomain}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.Status")}: {CertificateStatus}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.Subject")}: {Subject}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.Issuer")}: {Issuer}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.CommonName")}: {CommonName}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.Validity")}: {ValidityText}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.RemainingDays")}: {RemainingDays}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.SerialNumber")}: {SerialNumber}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.SignatureAlgorithm")}: {SignatureAlgorithm}");
        b.AppendLine($"{_localizer.Get("L.SslChecker.Column.TlsProtocol")}: {TlsProtocol}");

        if (ChainIssues.Count > 0)
        {
            b.AppendLine($"{_localizer.Get("L.SslChecker.Column.ChainIssues")}:");
            foreach (var issue in ChainIssues)
                b.AppendLine($"  - {issue}");
        }

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
            _logger.LogInformation("SSL check {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("SSL check {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}