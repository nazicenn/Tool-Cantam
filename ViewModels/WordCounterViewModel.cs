using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class WordCounterViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private int _characterCount = 0;

    [ObservableProperty]
    private int _characterCountNoSpaces = 0;

    [ObservableProperty]
    private int _wordCount = 0;

    [ObservableProperty]
    private int _sentenceCount = 0;

    [ObservableProperty]
    private int _paragraphCount = 0;

    [ObservableProperty]
    private int _lineCount = 0;

    [ObservableProperty]
    private string _readingTime = "0 min";

    [ObservableProperty]
    private string _speakingTime = "0 min";

    [ObservableProperty]
    private string _topWords = string.Empty;

    [ObservableProperty]
    private string _statusText = "Start typing to see statistics...";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public WordCounterViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

    partial void OnInputTextChanged(string value)
    {
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            ResetStatistics();
            StatusText = "Start typing to see statistics...";
            StatusType = NotificationType.Info;
            return;
        }

        // Karakter sayısı
        CharacterCount = InputText.Length;
        CharacterCountNoSpaces = InputText.Count(c => !char.IsWhiteSpace(c));

        // Kelime sayısı
        var words = Regex.Split(InputText.Trim(), @"\s+")
                         .Where(w => !string.IsNullOrWhiteSpace(w))
                         .ToArray();
        WordCount = words.Length;

        // Cümle sayısı (. ! ?)
        SentenceCount = Regex.Matches(InputText, @"[.!?]+").Count;
        if (SentenceCount == 0 && !string.IsNullOrWhiteSpace(InputText))
            SentenceCount = 1;

        // Paragraf sayısı (boş satırlarla ayrılmış)
        ParagraphCount = InputText.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                                  .Count(p => !string.IsNullOrWhiteSpace(p));
        if (ParagraphCount == 0 && !string.IsNullOrWhiteSpace(InputText))
            ParagraphCount = 1;

        // Satır sayısı
        LineCount = InputText.Split('\n').Length;

        // Okuma süresi (200 kelime/dk)
        double readingMinutes = WordCount / 200.0;
        ReadingTime = FormatTime(readingMinutes);

        // Konuşma süresi (130 kelime/dk)
        double speakingMinutes = WordCount / 130.0;
        SpeakingTime = FormatTime(speakingMinutes);

        // En çok kullanılan kelimeler (top 5)
        var topWordsList = words
            .Select(w => w.ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')'))
            .Where(w => w.Length > 2 && !IsStopWord(w))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToArray();

        TopWords = topWordsList.Length > 0
            ? string.Join(", ", topWordsList)
            : "No data yet";

        StatusText = $"✓ Updated: {WordCount} words, {CharacterCount} characters";
        StatusType = NotificationType.Success;
    }

    private void ResetStatistics()
    {
        CharacterCount = 0;
        CharacterCountNoSpaces = 0;
        WordCount = 0;
        SentenceCount = 0;
        ParagraphCount = 0;
        LineCount = 0;
        ReadingTime = "0 min";
        SpeakingTime = "0 min";
        TopWords = string.Empty;
    }

    private string FormatTime(double minutes)
    {
        if (minutes < 1)
            return $"{(int)(minutes * 60)} sec";
        if (minutes < 60)
            return $"{(int)minutes} min";

        int hours = (int)(minutes / 60);
        int mins = (int)(minutes % 60);
        return $"{hours}h {mins}m";
    }

    /// <summary>
    /// İngilizce etkisiz kelimeler. Her kelime için yeniden oluşturulmaması adına tek seferlik ve paylaşımlıdır.
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "are", "but", "not", "you", "all", "can", "her", "was", "one", "our", "out",
        "his", "has", "had", "him", "she", "her", "its", "who", "did", "yes", "how", "why", "what",
        "when", "where", "this", "that", "with", "from", "have", "will", "your", "they", "them", "then",
        "than", "into", "only", "also", "just", "some", "such", "like", "over", "back", "after", "been",
        "would", "could", "should", "there", "their", "these", "those", "about", "which", "while"
    };

    private static bool IsStopWord(string word) => StopWords.Contains(word);

    [RelayCommand]
    private void CopyStatistics()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            UpdateStatus("Nothing to copy", NotificationType.Warning);
            return;
        }

        var stats = $@"=== TEXT STATISTICS ===
Characters: {CharacterCount}
Characters (no spaces): {CharacterCountNoSpaces}
Words: {WordCount}
Sentences: {SentenceCount}
Paragraphs: {ParagraphCount}
Lines: {LineCount}
Reading Time: {ReadingTime}
Speaking Time: {SpeakingTime}
Top Words: {TopWords}";

        _clipboardService.CopyToClipboard(stats);
        UpdateStatus("✓ Statistics copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void CopyText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            UpdateStatus("Nothing to copy", NotificationType.Warning);
            return;
        }

        _clipboardService.CopyToClipboard(InputText);
        UpdateStatus("✓ Text copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        ResetStatistics();
        StatusText = "✓ Cleared";
        StatusType = NotificationType.Success;
    }

    private void UpdateStatus(string message, NotificationType type)
    {
        StatusText = message;
        StatusType = type;
    }
}