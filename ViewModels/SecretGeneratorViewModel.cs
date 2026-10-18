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

public partial class SecretGeneratorViewModel : ObservableObject
{
    private readonly ISecretGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SecretGeneratorViewModel> _logger;

    [ObservableProperty] private int _length = 32;
    [ObservableProperty] private SecretKind _kind = SecretKind.ApiKey;
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private string _generatedSecret = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public SecretGeneratorViewModel(ISecretGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<SecretGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.SecretGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.SecretGenerator.Limits", _service.MinLength, _service.MaxLength);
    public SecretKind[] Kinds => new[] { SecretKind.ApiKey, SecretKind.JwtSecret, SecretKind.RandomToken, SecretKind.Password, SecretKind.HexKey };

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.GenerateAsync(new SecretOptions(Length, Kind, Count), ct);
            if (!r.IsSuccess) { ReportFailure(r, "gen"); GeneratedSecret = string.Empty; return; }
            GeneratedSecret = r.Value!.Combined;
            _logger.LogInformation("Secret generated: {Kind} x{Count} len={Len}", Kind, Count, Length);
            SetStatus(NotificationType.Success, "L.SecretGenerator.Status.Generated", Kind, r.Value.Items.Count);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Secret gen failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopySecret()
    {
        if (string.IsNullOrWhiteSpace(GeneratedSecret)) { SetStatus(NotificationType.Warning, "L.SecretGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(GeneratedSecret); SetStatus(NotificationType.Success, "L.SecretGenerator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveSecret()
    {
        if (string.IsNullOrWhiteSpace(GeneratedSecret)) { SetStatus(NotificationType.Warning, "L.SecretGenerator.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.SecretGenerator.Filter.Text"), ".txt", "secrets");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, GeneratedSecret); SetStatus(NotificationType.Success, "L.SecretGenerator.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadSample() { Kind = SecretKind.ApiKey; Length = 32; Count = 1; GenerateCommand.Execute(null); }
    [RelayCommand] private void Clear() { GeneratedSecret = string.Empty; SetStatus(NotificationType.Info, "L.SecretGenerator.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("SecretGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}