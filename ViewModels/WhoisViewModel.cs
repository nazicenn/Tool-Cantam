using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// WHOIS Lookup aracının sunum katmanı. İş mantığı <see cref="IWhoisService"/> içindedir.
/// Sorgu async, iptal edilebilir; sonuç ayrıştırılmış alanlar + ham metin olarak gösterilir.
/// </summary>
public partial class WhoisViewModel : ObservableObject
{
    private readonly IWhoisService _whoisService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<WhoisViewModel> _logger;

    [ObservableProperty] private string _targetDomain = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    [ObservableProperty] private string _registrar = string.Empty;
    [ObservableProperty] private string _creationDate = string.Empty;
    [ObservableProperty] private string _expirationDate = string.Empty;
    [ObservableProperty] private string _updatedDate = string.Empty;
    [ObservableProperty] private string _nameServersText = string.Empty;
    [ObservableProperty] private string _statusesText = string.Empty;
    [ObservableProperty] private string _registrantName = string.Empty;
    [ObservableProperty] private string _registrantOrganization = string.Empty;
    [ObservableProperty] private string _registrantCountry = string.Empty;
    [ObservableProperty] private string _registrantEmail = string.Empty;
    [ObservableProperty] private string _whoisServer = string.Empty;
    [ObservableProperty] private string _rawText = string.Empty;

    public event Action? RequestTargetFocus;

    public WhoisViewModel(
        IWhoisService whoisService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<WhoisViewModel> logger)
    {
        _whoisService = whoisService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Whois.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.Whois.Limits", _whoisService.QueryTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LookupAsync(CancellationToken cancellationToken)
    {
        string target = TargetDomain?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Whois.Validation.InvalidDomain");
            return;
        }

        IsLoading = true;
        ClearResults();
        HasResults = false;

        try
        {
            SetStatus(NotificationType.Info, "L.Whois.Status.Querying", target);

            ToolResult<WhoisReport> result = await _whoisService.LookupAsync(
                target, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "whois");
                return;
            }

            WhoisReport report = result.Value!;

            Registrar = report.Registrar;
            CreationDate = report.CreationDate;
            ExpirationDate = report.ExpirationDate;
            UpdatedDate = report.UpdatedDate;
            NameServersText = report.NameServersText;
            StatusesText = report.StatusesText;
            RegistrantName = report.RegistrantName;
            RegistrantOrganization = report.RegistrantOrganization;
            RegistrantCountry = report.RegistrantCountry;
            RegistrantEmail = report.RegistrantEmail;
            WhoisServer = report.WhoisServer;
            RawText = report.RawText;
            HasResults = true;

            SummaryText = _localizer.Format("L.Whois.Status.Summary",
                report.Domain,
                report.Tld,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "WHOIS lookup finished: domain={Domain} tld={Tld} registrar={Registrar} elapsedMs={Elapsed}",
                report.Domain, report.Tld, report.Registrar, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.Whois.Status.Finished", report.Domain);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WHOIS lookup cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WHOIS lookup failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.Whois.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildSummary());
            SetStatus(NotificationType.Success, "L.Whois.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WHOIS result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (!HasResults)
        {
            SetStatus(NotificationType.Warning, "L.Whois.Validation.NothingToSave");
            return;
        }

        IsLoading = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                RawText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                $"{TargetDomain}-whois");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Whois.Status.Saved" : "L.Whois.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WHOIS result could not be saved.");
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

        TargetDomain = string.Empty;
        ClearResults();
        HasResults = false;
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.Whois.Status.Cleared");
    }

    private void ClearResults()
    {
        Registrar = string.Empty;
        CreationDate = string.Empty;
        ExpirationDate = string.Empty;
        UpdatedDate = string.Empty;
        NameServersText = string.Empty;
        StatusesText = string.Empty;
        RegistrantName = string.Empty;
        RegistrantOrganization = string.Empty;
        RegistrantCountry = string.Empty;
        RegistrantEmail = string.Empty;
        WhoisServer = string.Empty;
        RawText = string.Empty;
    }

    private string BuildSummary()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.Whois.Column.Domain")}: {TargetDomain}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.WhoisServer")}: {WhoisServer}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.Registrar")}: {Registrar}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.CreationDate")}: {CreationDate}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.ExpirationDate")}: {ExpirationDate}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.UpdatedDate")}: {UpdatedDate}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.NameServers")}: {NameServersText}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.Statuses")}: {StatusesText}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.RegistrantOrg")}: {RegistrantOrganization}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.RegistrantName")}: {RegistrantName}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.RegistrantCountry")}: {RegistrantCountry}");
        b.AppendLine($"{_localizer.Get("L.Whois.Column.RegistrantEmail")}: {RegistrantEmail}");
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
            _logger.LogInformation("WHOIS {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("WHOIS {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}