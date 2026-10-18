using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Text Diff sunum katmanı.</summary>
public partial class TextDiffViewModel : ObservableObject
{
    private readonly ITextDiffService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<TextDiffViewModel> _logger;

    [ObservableProperty] private string _leftText = string.Empty;
    [ObservableProperty] private string _rightText = string.Empty;
    [ObservableProperty] private string _diffResult = string.Empty;
    [ObservableProperty] private bool _ignoreCase;
    [ObservableProperty] private bool _ignoreWhitespace;
    [ObservableProperty] private bool _ignoreEmptyLines;
    [ObservableProperty] private int _addedLines;
    [ObservableProperty] private int _removedLines;
    [ObservableProperty] private int _changedLines;
    [ObservableProperty] private int _unchangedLines;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<DiffRow> DiffLines { get; } = new();

    public TextDiffViewModel(
        ITextDiffService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<TextDiffViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.TextDiff.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.TextDiff.Limits", _service.MaxInputLength / (1024 * 1024));

    partial void OnLeftTextChanged(string value) => CompareCommand.NotifyCanExecuteChanged();
    partial void OnRightTextChanged(string value) => CompareCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanCompare))]
    private async Task CompareAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new DiffOptions(IgnoreCase, IgnoreWhitespace, IgnoreEmptyLines);
            var result = await _service.CompareAsync(LeftText, RightText, opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "compare");
                DiffLines.Clear();
                DiffResult = string.Empty;
                ResetCounts();
                return;
            }

            var r = result.Value!;
            DiffLines.Clear();
            var sb = new System.Text.StringBuilder();

            foreach (var l in r.Lines)
            {
                var row = new DiffRow(
                    Kind: l.Kind.ToString(),
                    Sign: l.Kind switch
                    {
                        DiffLineKind.Added => "+",
                        DiffLineKind.Removed => "-",
                        DiffLineKind.Changed => "~",
                        _ => " "
                    },
                    LeftText: l.Left ?? "",
                    RightText: l.Right ?? "",
                    LeftIndex: l.LeftIndex,
                    RightIndex: l.RightIndex);

                DiffLines.Add(row);

                var text = l.Kind switch
                {
                    DiffLineKind.Added => $"+ {l.Right}",
                    DiffLineKind.Removed => $"- {l.Left}",
                    DiffLineKind.Changed => $"~ {l.Left} -> {l.Right}",
                    _ => $"  {l.Left}"
                };
                sb.AppendLine(text);
            }

            DiffResult = sb.ToString().TrimEnd();
            AddedLines = r.Added;
            RemovedLines = r.Removed;
            ChangedLines = r.Changed;
            UnchangedLines = r.Unchanged;

            _logger.LogInformation("TextDiff: +{A} -{R} ~{C} ={U} elapsed={Ms:0}ms",
                r.Added, r.Removed, r.Changed, r.Unchanged, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.TextDiff.Status.Compared",
                r.Added, r.Removed, r.Changed);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TextDiff failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanCompare() => !string.IsNullOrEmpty(LeftText) || !string.IsNullOrEmpty(RightText);

    [RelayCommand]
    private void Swap()
    {
        (LeftText, RightText) = (RightText, LeftText);
        SetStatus(NotificationType.Info, "L.TextDiff.Status.Swapped");
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(DiffResult))
        {
            SetStatus(NotificationType.Warning, "L.TextDiff.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(DiffResult);
            SetStatus(NotificationType.Success, "L.TextDiff.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveResult()
    {
        if (string.IsNullOrWhiteSpace(DiffResult))
        {
            SetStatus(NotificationType.Warning, "L.TextDiff.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.TextDiff.Filter.Text"), ".diff", "diff");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, DiffResult);
            SetStatus(NotificationType.Success, "L.TextDiff.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        LeftText = "Merhaba dünya\nBugün hava çok güzel\nBu bir testtir";
        RightText = "Merhaba dünya\nBugün hava çok sıcak\nBu bir testtir\nYeni satır";
        CompareCommand.Execute(null);
    }

    [RelayCommand]
    private void Clear()
    {
        LeftText = string.Empty;
        RightText = string.Empty;
        DiffResult = string.Empty;
        DiffLines.Clear();
        ResetCounts();
        SetStatus(NotificationType.Info, "L.TextDiff.Status.Cleared");
    }

    private void ResetCounts()
    {
        AddedLines = 0;
        RemovedLines = 0;
        ChangedLines = 0;
        UnchangedLines = 0;
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("TextDiff {Op} failed: kind={Kind} code={Code}",
            op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}

public sealed record DiffRow(string Kind, string Sign, string LeftText, string RightText, int LeftIndex, int RightIndex);