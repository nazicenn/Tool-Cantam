using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class LoremIpsumViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

    /// <summary>Başarı mesajının ekranda kalma süresi (ms).</summary>
    private const int StatusResetDelayMs = 3000;

    private static readonly string[] LatinWords =
    {
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
        "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
        "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud",
        "exercitation", "ullamco", "laboris", "nisi", "aliquip", "ex", "ea", "commodo",
        "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "fugiat", "nulla", "pariatur", "excepteur", "sint",
        "occaecat", "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia",
        "deserunt", "mollit", "anim", "id", "est", "laborum", "curabitur", "pretium",
        "tincidunt", "lacus", "nulla", "gravida", "orci", "a", "odio", "nullam",
        "varius", "turpis", "commodo", "condimentum", "lobortis", "feugiat", "vivamus",
        "elementum", "semper", "nisi", "aenean", "vulputate", "eleifend", "tellus"
    };

    [ObservableProperty]
    private int _paragraphCount = 3;

    [ObservableProperty]
    private int _sentenceCount = 5;

    [ObservableProperty]
    private bool _startWithLorem = true;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private NotificationType _statusType = NotificationType.Info;

    public LoremIpsumViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

    [RelayCommand]
    private void Generate()
    {
        try
        {
            ParagraphCount = Math.Clamp(ParagraphCount, 1, 50);
            SentenceCount = Math.Clamp(SentenceCount, 1, 20);

            var result = new StringBuilder();

            for (int p = 0; p < ParagraphCount; p++)
            {
                for (int s = 0; s < SentenceCount; s++)
                {
                    string sentence = GenerateSentence();

                    // İlk paragrafın ilk cümlesi "Lorem ipsum dolor sit amet" ile başlasın
                    if (p == 0 && s == 0 && StartWithLorem)
                    {
                        sentence = "Lorem ipsum dolor sit amet, " + char.ToLowerInvariant(sentence[0]) + sentence.Substring(1);
                    }

                    result.Append(sentence);
                    result.Append(' ');
                }

                result.AppendLine();
                result.AppendLine();
            }

            OutputText = result.ToString().Trim();
            UpdateStatus($"✓ {ParagraphCount} paragraph(s) generated", NotificationType.Success);
        }
        catch (Exception ex)
        {
            UpdateStatus($"✗ Generation failed: {ex.Message}", NotificationType.Error);
        }
    }

    private string GenerateSentence()
    {
        int wordCount = Random.Shared.Next(8, 18);
        var words = new string[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            words[i] = LatinWords[Random.Shared.Next(LatinWords.Length)];
        }

        string sentence = string.Join(" ", words);
        sentence = char.ToUpperInvariant(sentence[0]) + sentence.Substring(1);
        return sentence + ".";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            UpdateStatus("Nothing to copy. Generate text first.", NotificationType.Warning);
            return;
        }

        _clipboardService.CopyToClipboard(OutputText);
        UpdateStatus("✓ Copied to clipboard", NotificationType.Success);
    }

    [RelayCommand]
    private void Clear()
    {
        OutputText = string.Empty;
        UpdateStatus("✓ Cleared", NotificationType.Success);
    }

    private void UpdateStatus(string message, NotificationType type)
    {
        StatusText = message;
        StatusType = type;

        if (type == NotificationType.Success)
        {
            ResetStatusLaterAsync(message);
        }
    }

    /// <summary>
    /// Başarı mesajını bir süre sonra temizler. Bekleme sonrası devam işi UI iş parçacığına döner;
    /// böylece arayüz bağlı özellikler arka plan iş parçacığından yazılmaz.
    /// </summary>
    private async void ResetStatusLaterAsync(string message)
    {
        try
        {
            await Task.Delay(StatusResetDelayMs);

            if (StatusText == message)
            {
                StatusText = "Ready.";
                StatusType = NotificationType.Info;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Status reset failed: {ex.Message}");
        }
    }
}