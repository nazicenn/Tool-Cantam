using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class TextCounterViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private int _characterCount;

    [ObservableProperty]
    private int _characterCountNoSpaces;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private int _paragraphCount;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public TextCounterViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    partial void OnInputTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            CharacterCount = 0;
            CharacterCountNoSpaces = 0;
            WordCount = 0;
            LineCount = 0;
            ParagraphCount = 0;
            StatusText = "Enter text to count";
            return;
        }

        CharacterCount = value.Length;
        CharacterCountNoSpaces = value.Replace(" ", "").Replace("\n", "").Replace("\r", "").Length;
        WordCount = Regex.Matches(value, @"\b\w+\b").Count;
        LineCount = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
        ParagraphCount = value.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries).Length;

        StatusText = "Count completed";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        var results = $"Text Counter Results:\n\n" +
                      $"Characters: {CharacterCount}\n" +
                      $"Characters (no spaces): {CharacterCountNoSpaces}\n" +
                      $"Words: {WordCount}\n" +
                      $"Lines: {LineCount}\n" +
                      $"Paragraphs: {ParagraphCount}";

        _fileExportService.CopyToClipboard(results);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        CharacterCount = 0;
        CharacterCountNoSpaces = 0;
        WordCount = 0;
        LineCount = 0;
        ParagraphCount = 0;
        StatusText = "Cleared. Ready.";
    }
}