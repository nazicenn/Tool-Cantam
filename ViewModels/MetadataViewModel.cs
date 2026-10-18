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

/// <summary>
/// Metadata Cleaner aracının sunum katmanı. Tüm iş mantığı <see cref="IMetadataService"/> içindedir.
/// </summary>
public partial class MetadataViewModel : ObservableObject
{
    private readonly IMetadataService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<MetadataViewModel> _logger;

    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _fileInfoText = string.Empty;

    [ObservableProperty] private bool _cleanExif = true;
    [ObservableProperty] private bool _cleanGps = true;
    [ObservableProperty] private bool _cleanXmp = true;
    [ObservableProperty] private bool _cleanIptc = true;
    [ObservableProperty] private bool _cleanComments = true;
    [ObservableProperty] private bool _cleanThumbnail = true;

    public ObservableCollection<MetadataRow> MetadataRows { get; } = new();

    public MetadataViewModel(
        IMetadataService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<MetadataViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.Metadata.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.Metadata.Limits",
        _service.MaxFileSize / (1024 * 1024),
        string.Join(", ", _service.SupportedExtensions));

    /// <summary>
    /// XAML'de gösterilecek dosya yolu. Boşsa lokalize "No file selected" döner.
    /// (TargetNullValue + DynamicResource XAML'de çalışmadığı için burada çözülür.)
    /// </summary>
    public string FilePathDisplay =>
        string.IsNullOrWhiteSpace(FilePath)
            ? _localizer.Get("L.Metadata.NoFile")
            : FilePath;

    partial void OnFilePathChanged(string value)
    {
        MetadataRows.Clear();
        FileInfoText = string.Empty;
        OnPropertyChanged(nameof(FilePathDisplay));
        AnalyzeCommand.NotifyCanExecuteChanged();
        CleanCommand.NotifyCanExecuteChanged();
    }

    partial void OnCleanExifChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();
    partial void OnCleanGpsChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();
    partial void OnCleanXmpChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();
    partial void OnCleanIptcChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();
    partial void OnCleanCommentsChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();
    partial void OnCleanThumbnailChanged(bool value) => CleanCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void SelectFile()
    {
        var filter = _localizer.Get("L.Metadata.Filter.ImageFiles");
        var title = _localizer.Get("L.Metadata.Dialog.SelectFile");
        var path = _fileExportService.RequestOpenPath(filter, title);
        if (!string.IsNullOrWhiteSpace(path))
        {
            FilePath = path;
            SetStatus(NotificationType.Info, "L.Metadata.Status.FileSelected", Path.GetFileName(path));
        }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExecuteAnalyze))]
    private async Task AnalyzeAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.AnalyzeAsync(FilePath, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "analyze");
                return;
            }

            var report = result.Value!;
            MetadataRows.Clear();
            foreach (var entry in report.Entries)
                MetadataRows.Add(new MetadataRow(entry.Directory, entry.TagName, entry.Value));

            FileInfoText = $"{report.Format} · {report.FileSize:N0} B · {report.Entries.Count}";

            _logger.LogInformation("Metadata analyzed: format={Format} size={Size} entries={Entries}",
                report.Format, report.FileSize, report.Entries.Count);

            SetStatus(NotificationType.Success, "L.Metadata.Status.Analyzed", report.Entries.Count);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metadata analyze failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteAnalyze() => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExecuteClean))]
    private async Task CleanAsync(CancellationToken cancellationToken)
    {
        var options = new MetadataCleanOptions(
            CleanExif, CleanGps, CleanXmp, CleanIptc, CleanComments, CleanThumbnail);

        var defaultName = $"cleaned_{Path.GetFileName(FilePath)}";
        var destPath = _fileExportService.RequestSavePath(
            _localizer.Get("L.Metadata.Filter.ImageFiles"),
            Path.GetExtension(FilePath),
            defaultName);
        if (string.IsNullOrEmpty(destPath))
            return;

        IsBusy = true;
        try
        {
            var result = await _service.CleanAsync(FilePath, destPath, options, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "clean");
                return;
            }

            var report = result.Value!;
            _logger.LogInformation("Metadata cleaned: removed={Removed} orig={Orig} cleaned={Cleaned} elapsed={Ms}ms",
                report.FieldsRemoved, report.OriginalBytes, report.CleanedBytes, report.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.Metadata.Status.Cleaned",
                report.FieldsRemoved, report.OriginalBytes, report.CleanedBytes);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metadata clean failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteClean()
        => !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath)
           && (CleanExif || CleanGps || CleanXmp || CleanIptc || CleanComments || CleanThumbnail);

    [RelayCommand]
    private void SelectAll()
    {
        CleanExif = CleanGps = CleanXmp = CleanIptc = CleanComments = CleanThumbnail = true;
    }

    [RelayCommand]
    private void ClearAll()
    {
        CleanExif = CleanGps = CleanXmp = CleanIptc = CleanComments = CleanThumbnail = false;
    }

    [RelayCommand]
    private void Clear()
    {
        FilePath = string.Empty;
        MetadataRows.Clear();
        FileInfoText = string.Empty;
        SetStatus(NotificationType.Info, "L.Metadata.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("Metadata {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}

/// <summary>UI'da gösterilecek metadata satırı.</summary>
public sealed record MetadataRow(string Directory, string TagName, string Value);