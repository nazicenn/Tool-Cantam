using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using Wpf.Ui.Input;

namespace tool_cantam.ViewModels;

/// <summary>QR Code Reader sunum katmanı.</summary>
public partial class QrCodeReaderViewModel : ObservableObject
{
    private readonly IQrCodeReaderService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<QrCodeReaderViewModel> _logger;

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _qrText = string.Empty;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private string _format = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public QrCodeReaderViewModel(
        IQrCodeReaderService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<QrCodeReaderViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.QrCodeReader.Status.Ready");
    }

    public string FilePathDisplay => string.IsNullOrWhiteSpace(FilePath)
        ? _localizer.Get("L.QrCodeReader.NoFile")
        : FilePath;

    public string LimitsText => _localizer.Format("L.QrCodeReader.Limits",
        _service.MaxFileSize / (1024 * 1024),
        string.Join(", ", _service.SupportedExtensions));

    partial void OnFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(FilePathDisplay));
        ReadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectFile()
    {
        var path = _fileExportService.RequestOpenPath(
            _localizer.Get("L.QrCodeReader.Filter.Image"),
            _localizer.Get("L.QrCodeReader.Dialog.SelectFile"));
        if (!string.IsNullOrWhiteSpace(path))
        {
            FilePath = path;
            SetStatus(NotificationType.Info, "L.QrCodeReader.Status.FileSelected", Path.GetFileName(path));
            LoadPreview();
        }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExecuteRead))]
    private async Task ReadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.ReadAsync(FilePath, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "read");
                QrText = string.Empty;
                Format = string.Empty;
                return;
            }

            var r = result.Value!;
            QrText = r.Text;
            Format = r.Format;
            _logger.LogInformation("QR read ok: format={Format} length={Len}", r.Format, r.Text.Length);
            SetStatus(NotificationType.Success, "L.QrCodeReader.Status.Read", r.Format);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR read failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanExecuteRead() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(QrText))
        {
            SetStatus(NotificationType.Warning, "L.QrCodeReader.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(QrText);
            SetStatus(NotificationType.Success, "L.QrCodeReader.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clipboard failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        FilePath = string.Empty;
        QrText = string.Empty;
        Format = string.Empty;
        PreviewImage = null;
        SetStatus(NotificationType.Info, "L.QrCodeReader.Status.Cleared");
    }

    private void LoadPreview()
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(FilePath, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            PreviewImage = bmp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preview load failed.");
            PreviewImage = null;
        }
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("QrReader {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}