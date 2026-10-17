using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Arayüzde gösterilecek kelime-frekans satırı.</summary>
public sealed record WordFrequencyRow(string Word, int Count);

/// <summary>
/// Text Counter aracının sunum katmanı. İş mantığı <see cref="ITextCounterService"/> içindedir.
/// Girdi değiştikçe otomatik güncellenir; tüm sayımlar tek servis çağrısıyla gelir.
/// </summary>
public partial class TextCounterViewModel : ObservableObject
{
    private readonly ITextCounterService _counterService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<TextCounterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private int _characterCount;
    [ObservableProperty] private int _characterCountNoSpaces;
    [ObservableProperty] private int _wordCount;
    [ObservableProperty] private int _lineCount;
    [ObservableProperty] private int _paragraphCount;
    [ObservableProperty] private int _sentenceCount;
    [ObservableProperty] private string _readingTime = "-";
    [ObservableProperty] private string _speakingTime = "-";
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public System.Collections.ObjectModel.ObservableCollection<WordFrequencyRow> TopWords { get; } = new();

    public TextCounterViewModel(
        ITextCounterService counterService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<TextCounterViewModel> logger)
    {
        _counterService = counterService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.TextCounter.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.TextCounter.Limits", _counterService.MaxTextLength);

    partial void OnInputTextChanged(string value)
    {
        UpdateCounts(value);
    }

    private void UpdateCounts(string? text)
    {
        TopWords.Clear();

        if (string.IsNullOrEmpty(text))
        {
            ResetCounts();
            SetStatus(NotificationType.Info, "L.TextCounter.Status.Ready");
            return;
        }

        ToolResult<TextCounterReport> result = _counterService.Count(text);

        if (!result.IsSuccess)
        {
            ResetCounts();
            SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
            _logger.LogWarning("Text count failed: kind={Kind} code={Code}",
                result.ErrorKind, result.ErrorCode);
            return;
        }

        var r = result.Value!;
        CharacterCount = r.CharacterCount;
        CharacterCountNoSpaces = r.CharacterCountNoSpaces;
        WordCount = r.WordCount;
        LineCount = r.LineCount;
        ParagraphCount = r.ParagraphCount;
        SentenceCount = r.SentenceCount;
        ReadingTime = FormatDuration(r.ReadingTime);
        SpeakingTime = FormatDuration(r.SpeakingTime);

        foreach (var word in r.TopWords)
            TopWords.Add(new WordFrequencyRow(word.Word, word.Count));

        SetStatus(NotificationType.Success, "L.TextCounter.Status.Updated",
            r.WordCount, r.CharacterCount);
    }

    private void ResetCounts()
    {
        CharacterCount = 0;
        CharacterCountNoSpaces = 0;
        WordCount = 0;
        LineCount = 0;
        ParagraphCount = 0;
        SentenceCount = 0;
        ReadingTime = "-";
        SpeakingTime = "-";
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalSeconds < 1) return "< 1 sec";
        if (span.TotalMinutes < 1) return $"{(int)span.TotalSeconds} sec";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min";
        return $"{(int)span.TotalHours}h {span.Minutes}m";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            SetStatus(NotificationType.Warning, "L.TextCounter.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.TextCounter.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text count could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultsAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            SetStatus(NotificationType.Warning, "L.TextCounter.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "text-stats");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.TextCounter.Status.Saved" : "L.TextCounter.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text count could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        SetStatus(NotificationType.Info, "L.TextCounter.Status.Cleared");
    }

    private string BuildReport()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.Characters")}: {CharacterCount}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.CharactersNoSpaces")}: {CharacterCountNoSpaces}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.Words")}: {WordCount}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.Sentences")}: {SentenceCount}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.Lines")}: {LineCount}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.Paragraphs")}: {ParagraphCount}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.ReadingTime")}: {ReadingTime}");
        b.AppendLine($"{_localizer.Get("L.TextCounter.Label.SpeakingTime")}: {SpeakingTime}");

        if (TopWords.Count > 0)
        {
            b.AppendLine();
            b.AppendLine($"{_localizer.Get("L.TextCounter.Label.TopWords")}:");
            foreach (var row in TopWords)
                b.AppendLine($"  {row.Word} ({row.Count})");
        }

        return b.ToString().TrimEnd();
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }
}