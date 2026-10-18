using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class RsaKeyGeneratorViewModel : ObservableObject
{
    private readonly IRsaKeyGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<RsaKeyGeneratorViewModel> _logger;

    [ObservableProperty] private int _keySize = 2048;
    [ObservableProperty] private RsaFormat _format = RsaFormat.Pem;
    [ObservableProperty] private string _publicKey = string.Empty;
    [ObservableProperty] private string _privateKey = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public RsaKeyGeneratorViewModel(IRsaKeyGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<RsaKeyGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.RsaKeyGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.RsaKeyGenerator.Limits", string.Join(", ", _service.AllowedKeySizes));
    public int[] KeySizes => _service.AllowedKeySizes;
    public RsaFormat[] Formats => new[] { RsaFormat.Pem, RsaFormat.Xml, RsaFormat.Base64 };

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.GenerateAsync(new RsaOptions(KeySize, Format), ct);
            if (!r.IsSuccess) { ReportFailure(r, "generate"); PublicKey = string.Empty; PrivateKey = string.Empty; return; }
            PublicKey = r.Value!.PublicKey;
            PrivateKey = r.Value.PrivateKey;
            _logger.LogInformation("RSA generated: {Size} bit, {Format}, {Ms:0}ms", r.Value.KeySize, r.Value.Format, r.Value.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.RsaKeyGenerator.Status.Generated", r.Value.KeySize);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "RSA generate failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyPublic()
    {
        if (string.IsNullOrWhiteSpace(PublicKey)) { SetStatus(NotificationType.Warning, "L.RsaKeyGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(PublicKey); SetStatus(NotificationType.Success, "L.RsaKeyGenerator.Status.CopiedPublic"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void CopyPrivate()
    {
        if (string.IsNullOrWhiteSpace(PrivateKey)) { SetStatus(NotificationType.Warning, "L.RsaKeyGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(PrivateKey); SetStatus(NotificationType.Success, "L.RsaKeyGenerator.Status.CopiedPrivate"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SavePublic()
    {
        if (string.IsNullOrWhiteSpace(PublicKey)) { SetStatus(NotificationType.Warning, "L.RsaKeyGenerator.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.RsaKeyGenerator.Filter.Key"), ".pub", "public");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, PublicKey); SetStatus(NotificationType.Success, "L.RsaKeyGenerator.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void SavePrivate()
    {
        if (string.IsNullOrWhiteSpace(PrivateKey)) { SetStatus(NotificationType.Warning, "L.RsaKeyGenerator.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.RsaKeyGenerator.Filter.Key"), ".key", "private");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, PrivateKey); SetStatus(NotificationType.Success, "L.RsaKeyGenerator.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void Clear() { PublicKey = string.Empty; PrivateKey = string.Empty; SetStatus(NotificationType.Info, "L.RsaKeyGenerator.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("RsaKeyGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}