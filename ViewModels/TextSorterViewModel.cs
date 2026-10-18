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

/// <summary>Text Sorter sunum katmanı.</summary>
public partial class TextSorterViewModel : ObservableObject
{
    private readonly ITextSorterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<TextSorterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private SortDirection _direction = SortDirection.Ascending;
    [ObservableProperty] private SortMode _mode = SortMode.Invariant;
    [ObservableProperty] private bool _ignoreCase = true;
    [ObservableProperty] private bool _removeEmptyLines;
    [ObservableProperty] private bool _trimLines = true;
    [ObservableProperty] private bool _removeDuplicates;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _lineInfoText = string.Empty;

    public TextSorterViewModel(
        ITextSorterService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<TextSorterViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.TextSorter.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.TextSorter.Limits", _service.MaxTextLength / (1024 * 1024));
    public SortDirection[] Directions => new[] { SortDirection.Ascending, SortDirection.Descending };
    public SortMode[] Modes => new[] { SortMode.Ordinal, SortMode.Invariant };

    partial void OnInputTextChanged(string value) => SortCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExecuteSort))]
    private async Task SortAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new TextSorterOptions(Direction, Mode, IgnoreCase, RemoveEmptyLines, TrimLines, RemoveDuplicates);
            var result = await _service.SortAsync(InputText, opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "sort");
                OutputText = string.Empty;
                LineInfoText = string.Empty;
                return;
            }

            var r = result.Value!;
            OutputText = r.Output;
            LineInfoText = _localizer.Format("L.TextSorter.LineInfo",
                r.InputLines, r.OutputLines, r.RemovedEmpty, r.RemovedDuplicates);

            _logger.LogInformation("Text sorted: {In}->{Out} lines, removedEmpty={E}, removedDup={D}, {Ms:0}ms",
                r.InputLines, r.OutputLines, r.RemovedEmpty, r.RemovedDuplicates, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.TextSorter.Status.Sorted", r.OutputLines);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text sort failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanExecuteSort() => !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.TextSorter.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.TextSorter.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.TextSorter.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.TextSorter.Filter.Text"), ".txt", "sorted");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, OutputText);
            SetStatus(NotificationType.Success, "L.TextSorter.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        LineInfoText = string.Empty;
        SetStatus(NotificationType.Info, "L.TextSorter.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("TextSorter {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}