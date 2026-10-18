using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class BcryptGeneratorViewModel : ObservableObject
{
    private readonly IBcryptGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<BcryptGeneratorViewModel> _logger;

    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _hashedPassword = string.Empty;
    [ObservableProperty] private int _workFactor = 10;
    [ObservableProperty] private string _verifyPassword = string.Empty;
    [ObservableProperty] private bool _isVerified;
    [ObservableProperty] private string _verifyResult = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public BcryptGeneratorViewModel(IBcryptGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<BcryptGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.BcryptGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.BcryptGenerator.Limits", _service.MinWorkFactor, _service.MaxWorkFactor);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanHash))]
    private async Task HashAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.HashAsync(Password, WorkFactor, ct);
            if (!r.IsSuccess) { ReportFailure(r, "hash"); HashedPassword = string.Empty; return; }
            HashedPassword = r.Value!.Hash;
            _logger.LogInformation("Bcrypt hash: factor={Factor} in {Ms:0}ms", r.Value.WorkFactor, r.Value.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.BcryptGenerator.Status.Hashed", r.Value.WorkFactor);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Hash failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanHash() => !string.IsNullOrEmpty(Password);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanVerify))]
    private async Task VerifyAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.VerifyAsync(VerifyPassword, HashedPassword, ct);
            if (!r.IsSuccess) { ReportFailure(r, "verify"); IsVerified = false; VerifyResult = string.Empty; return; }
            IsVerified = r.Value!.IsMatch;
            VerifyResult = _localizer.Get(IsVerified ? "L.BcryptGenerator.VerifyMatch" : "L.BcryptGenerator.VerifyMismatch");
            SetStatus(IsVerified ? NotificationType.Success : NotificationType.Warning,
                IsVerified ? "L.BcryptGenerator.Status.Verified" : "L.BcryptGenerator.Status.NotVerified");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Verify failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanVerify() => !string.IsNullOrEmpty(VerifyPassword) && !string.IsNullOrEmpty(HashedPassword);

    [RelayCommand] private void GenerateRandomPassword() { Password = _service.GeneratePassword(16); SetStatus(NotificationType.Info, "L.BcryptGenerator.Status.RandomGenerated"); }

    [RelayCommand]
    private void CopyHash()
    {
        if (string.IsNullOrWhiteSpace(HashedPassword)) { SetStatus(NotificationType.Warning, "L.BcryptGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(HashedPassword); SetStatus(NotificationType.Success, "L.BcryptGenerator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        Password = string.Empty; HashedPassword = string.Empty; VerifyPassword = string.Empty;
        IsVerified = false; VerifyResult = string.Empty;
        SetStatus(NotificationType.Info, "L.BcryptGenerator.Status.Cleared");
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("BcryptGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}