using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class CertificateDecoderViewModel : ObservableObject
{
    private readonly ICertificateDecoderService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CertificateDecoderViewModel> _logger;

    [ObservableProperty] private string _certificateInput = string.Empty;
    [ObservableProperty] private string _version = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _signatureAlgorithm = string.Empty;
    [ObservableProperty] private string _issuer = string.Empty;
    [ObservableProperty] private string _validFrom = string.Empty;
    [ObservableProperty] private string _validTo = string.Empty;
    [ObservableProperty] private string _subject = string.Empty;
    [ObservableProperty] private string _publicKeyAlgorithm = string.Empty;
    [ObservableProperty] private int _publicKeySize;
    [ObservableProperty] private string _thumbprint = string.Empty;
    [ObservableProperty] private bool _isValid;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<CertExtRow> Extensions { get; } = new();

    public CertificateDecoderViewModel(ICertificateDecoderService service, IFileExportService fe, ILocalizerService loc, ILogger<CertificateDecoderViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CertificateDecoder.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.CertificateDecoder.Limits", _service.MaxInputLength / (1024 * 1024));

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanDecode))]
    private async Task DecodeAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.DecodeAsync(CertificateInput, ct);
            if (!r.IsSuccess) { ReportFailure(r, "decode"); ClearResults(); return; }
            ApplyReport(r.Value!);
            SetStatus(r.Value!.IsCurrentlyValid ? NotificationType.Success : NotificationType.Warning,
                r.Value.IsCurrentlyValid ? "L.CertificateDecoder.Status.Valid" : "L.CertificateDecoder.Status.Invalid");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Certificate decode failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanDecode() => !string.IsNullOrWhiteSpace(CertificateInput);

    [RelayCommand]
    private void LoadFile()
    {
        var path = _fileExportService.RequestOpenPath(_localizer.Get("L.CertificateDecoder.Filter.Cert"), _localizer.Get("L.CertificateDecoder.Dialog.SelectFile"));
        if (string.IsNullOrEmpty(path)) return;
        CertificateInput = path;
        DecodeFromFileCommand.Execute(path);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DecodeFromFileAsync(string? path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        IsBusy = true;
        try
        {
            var r = await _service.LoadFromFileAsync(path, ct);
            if (!r.IsSuccess) { ReportFailure(r, "file"); ClearResults(); return; }
            ApplyReport(r.Value!);
            SetStatus(NotificationType.Success, "L.CertificateDecoder.Status.Loaded", Path.GetFileName(path));
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Load failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private void ApplyReport(CertificateReport v)
    {
        Version = v.Version;
        SerialNumber = v.SerialNumber;
        SignatureAlgorithm = v.SignatureAlgorithm;
        Issuer = v.Issuer;
        Subject = v.Subject;
        ValidFrom = v.ValidFrom.ToString("yyyy-MM-dd HH:mm:ss");
        ValidTo = v.ValidTo.ToString("yyyy-MM-dd HH:mm:ss");
        PublicKeyAlgorithm = v.PublicKeyAlgorithm;
        PublicKeySize = v.PublicKeySize;
        Thumbprint = v.Thumbprint;
        IsValid = v.IsCurrentlyValid;
        Extensions.Clear();
        foreach (var e in v.Extensions) Extensions.Add(new CertExtRow(e.Name, e.Value));
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (string.IsNullOrWhiteSpace(Subject)) { SetStatus(NotificationType.Warning, "L.CertificateDecoder.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder();
        sb.AppendLine($"Version: {Version}");
        sb.AppendLine($"Serial: {SerialNumber}");
        sb.AppendLine($"Algorithm: {SignatureAlgorithm}");
        sb.AppendLine($"Issuer: {Issuer}");
        sb.AppendLine($"Subject: {Subject}");
        sb.AppendLine($"Valid: {ValidFrom} → {ValidTo}");
        sb.AppendLine($"Public key: {PublicKeyAlgorithm} ({PublicKeySize} bit)");
        sb.AppendLine($"Thumbprint: {Thumbprint}");
        sb.AppendLine($"Status: {(IsValid ? "Valid" : "Invalid")}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.CertificateDecoder.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { CertificateInput = string.Empty; ClearResults(); SetStatus(NotificationType.Info, "L.CertificateDecoder.Status.Cleared"); }

    private void ClearResults()
    {
        Version = SerialNumber = SignatureAlgorithm = Issuer = Subject = string.Empty;
        ValidFrom = ValidTo = PublicKeyAlgorithm = Thumbprint = string.Empty;
        PublicKeySize = 0; IsValid = false; Extensions.Clear();
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CertificateDecoder {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record CertExtRow(string Name, string Value);