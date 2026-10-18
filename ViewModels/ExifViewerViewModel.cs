using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class ExifViewerViewModel : ObservableObject
{
    private readonly IExifViewerService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<ExifViewerViewModel> _logger;

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private BitmapImage? _previewImage;
    [ObservableProperty] private string _fileInfoText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<ExifDirRow> Directories { get; } = new();

    public ExifViewerViewModel(IExifViewerService service, IFileExportService fe, ILocalizerService loc, ILogger<ExifViewerViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.ExifViewer.Status.Ready");
    }

    public string FilePathDisplay => string.IsNullOrWhiteSpace(FilePath) ? _localizer.Get("L.ExifViewer.NoFile") : FilePath;
    public string LimitsText => _localizer.Format("L.ExifViewer.Limits", _service.MaxFileSize / (1024 * 1024), string.Join(", ", _service.SupportedExtensions));

    partial void OnFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(FilePathDisplay));
        ReadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectFile()
    {
        var path = _fileExportService.RequestOpenPath(_localizer.Get("L.ExifViewer.Filter.Image"), _localizer.Get("L.ExifViewer.Dialog.SelectFile"));
        if (!string.IsNullOrWhiteSpace(path)) { FilePath = path; LoadPreview(); SetStatus(NotificationType.Info, "L.ExifViewer.Status.FileSelected", Path.GetFileName(path)); }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanRead))]
    private async Task ReadAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ReadAsync(FilePath, ct);
            if (!r.IsSuccess) { ReportFailure(r, "read"); Directories.Clear(); return; }
            var v = r.Value!;
            Directories.Clear();
            foreach (var d in v.Directories)
                Directories.Add(new ExifDirRow(d.Name, new ObservableCollection<ExifTagRow>(System.Linq.Enumerable.Select(d.Tags, t => new ExifTagRow(t.Name, t.Value)))));
            FileInfoText = $"{v.Format} · {v.FileSize:N0} B · {v.TotalTags} tags";
            _logger.LogInformation("EXIF read: {Format} {Tags} tags in {Ms:0}ms", v.Format, v.TotalTags, v.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.ExifViewer.Status.Read", v.TotalTags);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "EXIF read failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanRead() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

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
        catch (Exception ex) { _logger.LogWarning(ex, "Preview load failed."); PreviewImage = null; }
    }

    [RelayCommand]
    private void CopyAll()
    {
        if (Directories.Count == 0) { SetStatus(NotificationType.Warning, "L.ExifViewer.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder();
        foreach (var d in Directories)
        {
            sb.AppendLine($"[{d.Name}]");
            foreach (var t in d.Tags) sb.AppendLine($"  {t.Name}: {t.Value}");
            sb.AppendLine();
        }
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.ExifViewer.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveAll()
    {
        if (Directories.Count == 0) { SetStatus(NotificationType.Warning, "L.ExifViewer.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.ExifViewer.Filter.Text"), ".txt", "exif");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder();
        foreach (var d in Directories)
        {
            sb.AppendLine($"[{d.Name}]");
            foreach (var t in d.Tags) sb.AppendLine($"  {t.Name}: {t.Value}");
            sb.AppendLine();
        }
        try { File.WriteAllText(path, sb.ToString()); SetStatus(NotificationType.Success, "L.ExifViewer.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void Clear() { FilePath = string.Empty; PreviewImage = null; Directories.Clear(); FileInfoText = string.Empty; SetStatus(NotificationType.Info, "L.ExifViewer.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("ExifViewer {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record ExifDirRow(string Name, ObservableCollection<ExifTagRow> Tags);
public sealed record ExifTagRow(string Name, string Value);