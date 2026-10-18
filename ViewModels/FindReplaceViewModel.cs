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

/// <summary>Find and Replace sunum katmanı.</summary>
public partial class FindReplaceViewModel : ObservableObject
{
    private readonly IFindReplaceService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<FindReplaceViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _findText = string.Empty;
    [ObservableProperty] private string _replaceText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private bool _matchCase;
    [ObservableProperty] private bool _matchWholeWord;
    [ObservableProperty] private bool _useRegex;
    [ObservableProperty] private int _replaceCount;
    [ObservableProperty] private int _matchCount;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public FindReplaceViewModel(
        IFindReplaceService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<FindReplaceViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.FindReplace.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.FindReplace.Limits", _service.MaxInputLength / (1024 * 1024));

    partial void OnInputTextChanged(string value) => ReplaceCommand.NotifyCanExecuteChanged();
    partial void OnFindTextChanged(string value) => ReplaceCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanReplace))]
    private async Task ReplaceAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new FindReplaceOptions(MatchCase, MatchWholeWord, UseRegex);
            var result = await _service.ReplaceAsync(InputText, FindText, ReplaceText, opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "replace");
                OutputText = string.Empty;
                MatchCount = 0;
                ReplaceCount = 0;
                return;
            }

            var r = result.Value!;
            OutputText = r.Output;
            MatchCount = r.MatchCount;
            ReplaceCount = r.ReplaceCount;

            _logger.LogInformation("FindReplace: matches={M} replaced={R} elapsed={Ms:0}ms",
                r.MatchCount, r.ReplaceCount, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.FindReplace.Status.Replaced", r.ReplaceCount);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FindReplace failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanReplace() => !string.IsNullOrEmpty(InputText) && !string.IsNullOrEmpty(FindText);

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.FindReplace.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.FindReplace.Status.Copied");
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
            SetStatus(NotificationType.Warning, "L.FindReplace.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.FindReplace.Filter.Text"), ".txt", "replaced");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, OutputText);
            SetStatus(NotificationType.Success, "L.FindReplace.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        MatchCount = 0;
        ReplaceCount = 0;
        SetStatus(NotificationType.Info, "L.FindReplace.Status.Swapped");
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        FindText = string.Empty;
        ReplaceText = string.Empty;
        OutputText = string.Empty;
        MatchCount = 0;
        ReplaceCount = 0;
        SetStatus(NotificationType.Info, "L.FindReplace.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("FindReplace {Op} failed: kind={Kind} code={Code}",
            op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}