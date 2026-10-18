using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>QR Code Generator sunum katmanı.</summary>
public partial class QrCodeViewModel : ObservableObject
{
    private readonly IQrCodeService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<QrCodeViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private BitmapImage? _qrCodeImage;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _pixelsPerModule = 20;
    [ObservableProperty] private QrCodeEcc _ecc = QrCodeEcc.Q;

    private byte[]? _lastPng;

    public QrCodeViewModel(
        IQrCodeService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<QrCodeViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.QrCode.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.QrCode.Limits", _service.MaxInputLength);
    public QrCodeEcc[] EccLevels => new[] { QrCodeEcc.L, QrCodeEcc.M, QrCodeEcc.Q, QrCodeEcc.H };

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.GenerateAsync(InputText, Ecc, PixelsPerModule, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "generate");
                QrCodeImage = null;
                _lastPng = null;
                return;
            }

            var report = result.Value!;
            _lastPng = report.PngBytes;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(report.PngBytes);
            image.EndInit();
            image.Freeze();
            QrCodeImage = image;

            _logger.LogInformation("QR generated: ecc={Ecc} size={Bytes}", report.Ecc, report.ByteLength);
            SetStatus(NotificationType.Success, "L.QrCode.Status.Generated", report.ByteLength);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR generate failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveAsImageAsync()
    {
        if (_lastPng is null)
        {
            SetStatus(NotificationType.Warning, "L.QrCode.Validation.NothingToSave");
            return;
        }

        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.QrCode.Filter.Png"),
            ".png",
            "qrcode");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            await File.WriteAllBytesAsync(path, _lastPng);
            SetStatus(NotificationType.Success, "L.QrCode.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        QrCodeImage = null;
        _lastPng = null;
        SetStatus(NotificationType.Info, "L.QrCode.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("QrCode {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}