// ViewModels/FindReplaceViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class FindReplaceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _findText = string.Empty;

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private bool _matchCase = false;

    [ObservableProperty]
    private bool _matchWholeWord = false;

    [ObservableProperty]
    private int _replaceCount = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Replace()
    {
        if (string.IsNullOrEmpty(InputText))
        {
            StatusText = "No input text";
            return;
        }

        if (string.IsNullOrEmpty(FindText))
        {
            StatusText = "Please enter text to find";
            return;
        }

        try
        {
            string result;
            int count;

            var comparison = MatchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (MatchWholeWord)
            {
                var pattern = @"\b" + Regex.Escape(FindText) + @"\b";
                var options = MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                result = Regex.Replace(InputText, pattern, ReplaceText, options);
                count = Regex.Matches(InputText, pattern, options).Count;
            }
            else
            {
                result = InputText.Replace(FindText, ReplaceText);
                count = (InputText.Length - result.Length) / Math.Max(FindText.Length, 1);
            }

            OutputText = result;
            ReplaceCount = count;
            StatusText = $"Replaced {count} occurrence(s)";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        FindText = string.Empty;
        ReplaceText = string.Empty;
        OutputText = string.Empty;
        ReplaceCount = 0;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            Clipboard.SetText(OutputText);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        StatusText = "Swapped";
    }
}