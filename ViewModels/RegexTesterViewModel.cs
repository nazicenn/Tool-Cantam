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

/// <summary>Regex Tester sunum katmanı.</summary>
public partial class RegexTesterViewModel : ObservableObject
{
    private readonly IRegexTesterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<RegexTesterViewModel> _logger;

    [ObservableProperty] private string _pattern = string.Empty;
    [ObservableProperty] private string _input = string.Empty;
    [ObservableProperty] private string _replaceText = string.Empty;
    [ObservableProperty] private string _highlighted = string.Empty;
    [ObservableProperty] private string _replaced = string.Empty;
    [ObservableProperty] private bool _ignoreCase;
    [ObservableProperty] private bool _multiline;
    [ObservableProperty] private bool _singleline;
    [ObservableProperty] private bool _explicitCapture;
    [ObservableProperty] private bool _ignorePatternWhitespace;
    [ObservableProperty] private bool _replaceMode;
    [ObservableProperty] private int _matchCount;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<MatchRow> Matches { get; } = new();
    public ObservableCollection<RegexSample> Samples { get; } = new();

    public RegexTesterViewModel(
        IRegexTesterService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<RegexTesterViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.RegexTester.Status.Ready");

        foreach (var s in _service.GetSamples())
            Samples.Add(s);
    }

    public string LimitsText => _localizer.Format("L.RegexTester.Limits",
        _service.MaxInputLength / (1024 * 1024),
        _service.MaxMatchCount,
        _service.RegexTimeout.TotalSeconds);

    partial void OnPatternChanged(string value) => TestCommand.NotifyCanExecuteChanged();
    partial void OnInputChanged(string value) => TestCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanTest))]
    private async Task TestAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new RegexTestOptions(
                IgnoreCase, Multiline, Singleline, ExplicitCapture, IgnorePatternWhitespace,
                ReplaceMode, ReplaceText);

            var result = await _service.TestAsync(Pattern, Input, opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "test");
                Matches.Clear();
                Highlighted = string.Empty;
                Replaced = string.Empty;
                MatchCount = 0;
                return;
            }

            var r = result.Value!;
            Matches.Clear();
            foreach (var m in r.Matches)
                Matches.Add(new MatchRow(m.Index, m.Length, m.Value, string.Join(" | ", m.Groups)));

            Highlighted = r.Highlighted;
            Replaced = r.Replaced;
            MatchCount = r.MatchCount;

            _logger.LogInformation("Regex test: matches={M} elapsed={Ms:0}ms", r.MatchCount, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success,
                r.MatchCount == 0 ? "L.RegexTester.Status.NoMatches" : "L.RegexTester.Status.Matched",
                r.MatchCount);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Regex test failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanTest() => !string.IsNullOrWhiteSpace(Pattern) && !string.IsNullOrEmpty(Input);

    [RelayCommand]
    private void LoadSample(RegexSample? sample)
    {
        if (sample is null) return;
        Pattern = sample.Pattern;
        Input = sample.Input;
        ReplaceText = sample.ReplaceWith;
        TestCommand.Execute(null);
    }

    [RelayCommand]
    private void CopyHighlighted()
    {
        if (string.IsNullOrWhiteSpace(Highlighted)) { SetStatus(NotificationType.Warning, "L.RegexTester.Validation.NothingToCopy"); return; }
        try
        {
            _fileExportService.CopyToClipboard(Highlighted);
            SetStatus(NotificationType.Success, "L.RegexTester.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveHighlighted()
    {
        if (string.IsNullOrWhiteSpace(Highlighted)) { SetStatus(NotificationType.Warning, "L.RegexTester.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.RegexTester.Filter.Text"), ".txt", "regex-result");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.WriteAllText(path, Highlighted);
            SetStatus(NotificationType.Success, "L.RegexTester.Status.Saved", Path.GetFileName(path));
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
        Pattern = string.Empty;
        Input = string.Empty;
        ReplaceText = string.Empty;
        Highlighted = string.Empty;
        Replaced = string.Empty;
        Matches.Clear();
        MatchCount = 0;
        SetStatus(NotificationType.Info, "L.RegexTester.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("RegexTester {Op} failed: kind={Kind} code={Code}",
            op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}

public sealed record MatchRow(int Index, int Length, string Value, string Groups);