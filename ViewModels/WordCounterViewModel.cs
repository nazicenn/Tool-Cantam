using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class WordCounterViewModel : ObservableObject
{
    private readonly IWordCounterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<WordCounterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private int _characterCount;
    [ObservableProperty] private int _characterCountNoSpaces;
    [ObservableProperty] private int _wordCount;
    [ObservableProperty] private int _sentenceCount;
    [ObservableProperty] private int _paragraphCount;
    [ObservableProperty] private int _lineCount;
    [ObservableProperty] private string _readingTime = "0";
    [ObservableProperty] private string _speakingTime = "0";
    [ObservableProperty] private string _topWords = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    private CancellationTokenSource? _debounceCts;

    public WordCounterViewModel(IWordCounterService service, IFileExportService fe, ILocalizerService loc, ILogger<WordCounterViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.WordCounter.Status.StartTyping");
    }

    public string LimitsText => _localizer.Format("L.WordCounter.Limits", _service.MaxInputLength / (1024 * 1024));

    partial void OnInputTextChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        _ = AnalyzeDebouncedAsync(value, _debounceCts.Token);
    }

    private async Task AnalyzeDebouncedAsync(string text, CancellationToken ct)
    {
        try
        {
            await Task.Delay(250, ct); // Debounce
            await AnalyzeAsync(text, ct);
        }
        catch (OperationCanceledException) { /* yeni tuşlama */ }
        catch (Exception ex) { _logger.LogError(ex, "Debounce failed."); }
    }

    private async Task AnalyzeAsync(string text, CancellationToken ct)
    {
        var r = await _service.AnalyzeAsync(text, ct);
        if (!r.IsSuccess) { SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); return; }

        var v = r.Value!;
        CharacterCount = v.Characters;
        CharacterCountNoSpaces = v.CharactersNoSpaces;
        WordCount = v.Words;
        SentenceCount = v.Sentences;
        ParagraphCount = v.Paragraphs;
        LineCount = v.Lines;
        ReadingTime = FormatTime(v.ReadingTime);
        SpeakingTime = FormatTime(v.SpeakingTime);
        TopWords = v.TopWords.Count == 0
            ? _localizer.Get("L.WordCounter.NoData")
            : string.Join(", ", System.Linq.Enumerable.Select(v.TopWords, x => $"{x.Word} ({x.Count})"));

        if (v.Words == 0)
            SetStatus(NotificationType.Info, "L.WordCounter.Status.StartTyping");
        else
            SetStatus(NotificationType.Success, "L.WordCounter.Status.Updated", v.Words, v.Characters);
    }

    private string FormatTime(TimeSpan t)
    {
        if (t.TotalSeconds < 1) return _localizer.Format("L.WordCounter.TimeFormat.Sec", 0);
        if (t.TotalMinutes < 1) return _localizer.Format("L.WordCounter.TimeFormat.Sec", (int)t.TotalSeconds);
        if (t.TotalHours < 1) return _localizer.Format("L.WordCounter.TimeFormat.Min", (int)t.TotalMinutes);
        return _localizer.Format("L.WordCounter.TimeFormat.HourMin", (int)t.TotalHours, t.Minutes);
    }

    [RelayCommand]
    private void CopyStatistics()
    {
        if (WordCount == 0) { SetStatus(NotificationType.Warning, "L.WordCounter.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder();
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Characters") + ": " + CharacterCount);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.CharactersNoSpaces") + ": " + CharacterCountNoSpaces);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Words") + ": " + WordCount);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Sentences") + ": " + SentenceCount);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Paragraphs") + ": " + ParagraphCount);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Lines") + ": " + LineCount);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Reading") + ": " + ReadingTime);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.Speaking") + ": " + SpeakingTime);
        sb.AppendLine(_localizer.Get("L.WordCounter.Label.TopWords") + ": " + TopWords);
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.WordCounter.Status.StatsCopied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void CopyText()
    {
        if (string.IsNullOrWhiteSpace(InputText)) { SetStatus(NotificationType.Warning, "L.WordCounter.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(InputText); SetStatus(NotificationType.Success, "L.WordCounter.Status.TextCopied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        SetStatus(NotificationType.Info, "L.WordCounter.Status.Cleared");
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
}