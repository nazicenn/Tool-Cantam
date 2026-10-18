using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class AesEncryptViewModel : ObservableObject
{
    private readonly IAesEncryptService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<AesEncryptViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public AesEncryptViewModel(IAesEncryptService service, IFileExportService fe, ILocalizerService loc, ILogger<AesEncryptViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.AesEncrypt.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.AesEncrypt.Limits", _service.MinPasswordLength);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanEncrypt))]
    private async Task EncryptAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.EncryptAsync(InputText, Password, ct);
            if (!r.IsSuccess) { ReportFailure(r, "encrypt"); OutputText = string.Empty; return; }
            OutputText = r.Value!.Output;
            _logger.LogInformation("AES encrypt: {In}->{Out}", r.Value.InputLength, r.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.AesEncrypt.Status.Encrypted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Encrypt failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanEncrypt() => !string.IsNullOrEmpty(InputText) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanEncrypt))]
    private async Task DecryptAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.DecryptAsync(InputText, Password, ct);
            if (!r.IsSuccess) { ReportFailure(r, "decrypt"); OutputText = string.Empty; return; }
            OutputText = r.Value!.Output;
            _logger.LogInformation("AES decrypt: {In}->{Out}", r.Value.InputLength, r.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.AesEncrypt.Status.Decrypted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Decrypt failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void GeneratePassword() => Password = _service.GeneratePassword(16);
    [RelayCommand] private void Swap() { (InputText, OutputText) = (OutputText, InputText); SetStatus(NotificationType.Info, "L.AesEncrypt.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText)) { SetStatus(NotificationType.Warning, "L.AesEncrypt.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputText); SetStatus(NotificationType.Success, "L.AesEncrypt.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { InputText = string.Empty; OutputText = string.Empty; Password = string.Empty; SetStatus(NotificationType.Info, "L.AesEncrypt.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("AesEncrypt {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}