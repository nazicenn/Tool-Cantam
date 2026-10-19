using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PwnedViewModel : ObservableObject
{
    private readonly IPwnedService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<PwnedViewModel> _logger;

    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _resultText = string.Empty;
    [ObservableProperty] private string _suggestedPassword = string.Empty;
    [ObservableProperty] private bool _showSuggestion;
    [ObservableProperty] private bool _isPwned;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public PwnedViewModel(IPwnedService service, IFileExportService fe, ILocalizerService loc, ILogger<PwnedViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.Pwned.Status.Ready");
    }

    public string LimitsText => _localizer.Get("L.Pwned.Limits");

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanCheck))]
    private async Task CheckAsync(CancellationToken ct)
    {
        IsBusy = true;
        ShowSuggestion = false;
        SuggestedPassword = string.Empty;
        try
        {
            var r = await _service.CheckPasswordAsync(Password, ct);
            if (!r.IsSuccess) { ReportFailure(r, "check"); ResultText = string.Empty; IsPwned = false; return; }
            var v = r.Value!;
            IsPwned = v.IsPwned;
            ResultText = v.IsPwned
                ? _localizer.Format("L.Pwned.Result.Pwned", v.BreachCount)
                : _localizer.Get("L.Pwned.Result.Safe");
            if (v.IsPwned)
            {
                SuggestedPassword = GenerateSuggestion(Password);
                ShowSuggestion = true;
            }
            _logger.LogInformation("HIBP check: {Status}", v.IsPwned ? "PWNED" : "SAFE");
            SetStatus(v.IsPwned ? NotificationType.Warning : NotificationType.Success,
                v.IsPwned ? "L.Pwned.Status.Pwned" : "L.Pwned.Status.Safe", v.BreachCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "HIBP check failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanCheck() => !string.IsNullOrEmpty(Password);

    private static string GenerateSuggestion(string pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd)) return "MySecurePass_123";
        var sb = new System.Text.StringBuilder(pwd.Trim());
        if (sb.Length < 8) sb.Append("Secure");
        if (!System.Text.RegularExpressions.Regex.IsMatch(sb.ToString(), @"[0-9]"))
            sb.Append(System.Security.Cryptography.RandomNumberGenerator.GetInt32(10, 999));
        if (!System.Text.RegularExpressions.Regex.IsMatch(sb.ToString(), @"[\W_]"))
            sb.Append('_');
        return sb.ToString();
    }

    [RelayCommand]
    private void UseSuggestion()
    {
        if (!string.IsNullOrWhiteSpace(SuggestedPassword))
        {
            Password = SuggestedPassword;
            SuggestedPassword = string.Empty;
            ShowSuggestion = false;
            ResultText = string.Empty;
            SetStatus(NotificationType.Info, "L.Pwned.Status.SuggestionApplied");
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(ResultText)) { SetStatus(NotificationType.Warning, "L.Pwned.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(ResultText); SetStatus(NotificationType.Success, "L.Pwned.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        Password = string.Empty; ResultText = string.Empty;
        SuggestedPassword = string.Empty; ShowSuggestion = false; IsPwned = false;
        SetStatus(NotificationType.Info, "L.Pwned.Status.Cleared");
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("Pwned {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}