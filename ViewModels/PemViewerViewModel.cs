using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PemViewerViewModel : ObservableObject
{
    private readonly IPemViewerService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<PemViewerViewModel> _logger;

    [ObservableProperty] private string _pemInput = string.Empty;
    [ObservableProperty] private string _subject = string.Empty;
    [ObservableProperty] private string _issuer = string.Empty;
    [ObservableProperty] private string _serialNumber = string.Empty;
    [ObservableProperty] private string _validFrom = string.Empty;
    [ObservableProperty] private string _validTo = string.Empty;
    [ObservableProperty] private string _publicKey = string.Empty;
    [ObservableProperty] private string _signatureAlgorithm = string.Empty;
    [ObservableProperty] private string _thumbprint = string.Empty;
    [ObservableProperty] private bool _isValid;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<PemBlockRow> Blocks { get; } = new();

    public PemViewerViewModel(IPemViewerService service, IFileExportService fe, ILocalizerService loc, ILogger<PemViewerViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.PemViewer.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.PemViewer.Limits", _service.MaxInputLength / (1024 * 1024));

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanParse))]
    private async Task ParseAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ParseAsync(PemInput, ct);
            if (!r.IsSuccess) { ReportFailure(r, "parse"); ClearResults(); return; }
            var v = r.Value!;
            Subject = v.Subject; Issuer = v.Issuer; SerialNumber = v.SerialNumber;
            ValidFrom = v.ValidFrom.ToString("yyyy-MM-dd HH:mm:ss");
            ValidTo = v.ValidTo.ToString("yyyy-MM-dd HH:mm:ss");
            PublicKey = v.PublicKey; SignatureAlgorithm = v.SignatureAlgorithm; Thumbprint = v.Thumbprint;
            IsValid = v.IsCurrentlyValid;
            Blocks.Clear();
            foreach (var b in v.Blocks) Blocks.Add(new PemBlockRow(b.Label, b.ByteCount));
            SetStatus(v.IsCurrentlyValid ? NotificationType.Success : NotificationType.Warning,
                v.IsCurrentlyValid ? "L.PemViewer.Status.Valid" : "L.PemViewer.Status.Invalid");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "PEM parse failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanParse() => !string.IsNullOrWhiteSpace(PemInput);

    [RelayCommand]
    private void LoadSample() { PemInput = "-----BEGIN CERTIFICATE-----\nMIIDdTCCAl2gAwIBAgIJAMkq5B2D7NQDMA0GCSqGSIb3DQEBCwUAMEUxCzAJBgNV\nBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMRswGQYDVQQDExJHcmFu\ndCBUZXN0IEVWIFJvb3QwHhcNMjQwMTAxMDAwMDAwWhcNMjUwMTAxMDAwMDAwWjBF\n-----END CERTIFICATE-----"; ParseCommand.Execute(null); }

    [RelayCommand]
    private void CopyAll()
    {
        if (string.IsNullOrWhiteSpace(Subject)) { SetStatus(NotificationType.Warning, "L.PemViewer.Validation.NothingToCopy"); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Subject: {Subject}");
        sb.AppendLine($"Issuer: {Issuer}");
        sb.AppendLine($"Serial: {SerialNumber}");
        sb.AppendLine($"Valid: {ValidFrom} → {ValidTo}");
        sb.AppendLine($"Public key: {PublicKey}");
        sb.AppendLine($"Signature: {SignatureAlgorithm}");
        sb.AppendLine($"Thumbprint: {Thumbprint}");
        sb.AppendLine($"Status: {(IsValid ? "Valid" : "Invalid")}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.PemViewer.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { PemInput = string.Empty; ClearResults(); SetStatus(NotificationType.Info, "L.PemViewer.Status.Cleared"); }

    private void ClearResults()
    {
        Subject = Issuer = SerialNumber = ValidFrom = ValidTo = string.Empty;
        PublicKey = SignatureAlgorithm = Thumbprint = string.Empty;
        IsValid = false; Blocks.Clear();
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("PemViewer {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record PemBlockRow(string Label, int ByteCount);