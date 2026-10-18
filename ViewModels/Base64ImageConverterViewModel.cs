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

public partial class Base64ImageConverterViewModel : ObservableObject
{
    private readonly IBase64ImageService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<Base64ImageConverterViewModel> _logger;

    [ObservableProperty] private string _base64Text = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private string _fileInfoText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public Base64ImageConverterViewModel(IBase64ImageService service, IFileExportService fe, ILocalizerService loc, ILogger<Base64ImageConverterViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.Base64Image.Status.Ready");
    }

    public string FilePathDisplay => string.IsNullOrWhiteSpace(FilePath) ? _localizer.Get("L.Base64Image.NoFile") : FilePath;
    public string LimitsText => _localizer.Format("L.Base64Image.Limits", _service.MaxFileSize / (1024 * 1024));

    partial void OnFilePathChanged(string value) => OnPropertyChanged(nameof(FilePathDisplay));

    [RelayCommand]
    private void SelectFile()
    {
        var path = _fileExportService.RequestOpenPath(_localizer.Get("L.Base64Image.Filter.Image"), _localizer.Get("L.Base64Image.Dialog.SelectFile"));
        if (!string.IsNullOrWhiteSpace(path)) { FilePath = path; ConvertImageToBase64Command.Execute(null); }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanConvertImage))]
    private async Task ConvertImageToBase64Async(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ImageToBase64Async(FilePath, ct);
            if (!r.IsSuccess) { ReportFailure(r, "img2b64"); Base64Text = string.Empty; PreviewImage = null; return; }
            Base64Text = r.Value!.Base64;
            FileInfoText = $"{r.Value.Format} · {r.Value.OriginalBytes:N0} B";
            LoadPreview();
            SetStatus(NotificationType.Success, "L.Base64Image.Status.Converted", r.Value.Format, r.Value.OriginalBytes);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Image→Base64 failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanConvertImage() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanConvertBase64))]
    private async Task ConvertBase64ToImageAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.Base64ToImageAsync(Base64Text, ct);
            if (!r.IsSuccess) { ReportFailure(r, "b642img"); PreviewImage = null; return; }
            var bytes = r.Value!.ImageBytes;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            PreviewImage = bmp;
            FileInfoText = $"{r.Value.DetectedFormat} · {r.Value.ByteCount:N0} B";
            SetStatus(NotificationType.Success, "L.Base64Image.Status.ConvertedBack", r.Value.DetectedFormat, r.Value.ByteCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Base64→Image failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanConvertBase64() => !string.IsNullOrWhiteSpace(Base64Text);

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
        catch (Exception ex) { _logger.LogWarning(ex, "Preview load failed."); }
    }

    [RelayCommand]
    private void CopyBase64()
    {
        if (string.IsNullOrWhiteSpace(Base64Text)) { SetStatus(NotificationType.Warning, "L.Base64Image.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(Base64Text); SetStatus(NotificationType.Success, "L.Base64Image.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { Base64Text = string.Empty; FilePath = string.Empty; PreviewImage = null; FileInfoText = string.Empty; SetStatus(NotificationType.Info, "L.Base64Image.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("Base64Image {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}