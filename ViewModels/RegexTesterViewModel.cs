// ViewModels/RegexTesterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class RegexTesterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _pattern = @"\b\w+@\w+\.\w+\b";

    [ObservableProperty]
    private string _input = "test@example.com, john.doe@test.co.uk, invalid-email";

    [ObservableProperty]
    private string _replaceText = string.Empty;

    [ObservableProperty]
    private string _output = string.Empty;

    [ObservableProperty]
    private bool _ignoreCase = false;

    [ObservableProperty]
    private bool _multiline = false;

    [ObservableProperty]
    private bool _replaceMode = false;

    [ObservableProperty]
    private int _matchCount = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Test()
    {
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            StatusText = "Please enter a regex pattern";
            return;
        }

        if (string.IsNullOrWhiteSpace(Input))
        {
            StatusText = "Please enter input text";
            return;
        }

        try
        {
            var options = RegexOptions.None;
            if (IgnoreCase) options |= RegexOptions.IgnoreCase;
            if (Multiline) options |= RegexOptions.Multiline;

            var regex = new Regex(Pattern, options);

            if (ReplaceMode)
            {
                var result = regex.Replace(Input, ReplaceText);
                Output = result;
                MatchCount = regex.Matches(Input).Count;
                StatusText = $"Replaced {MatchCount} match(es)";
            }
            else
            {
                var matches = regex.Matches(Input);
                MatchCount = matches.Count;

                var highlighted = regex.Replace(Input, m => $"[{m.Value}]");
                Output = highlighted;
                StatusText = $"Found {MatchCount} match(es)";
            }
        }
        catch (ArgumentException ex)
        {
            StatusText = $"Invalid regex: {ex.Message}";
            Output = string.Empty;
            MatchCount = 0;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Pattern = string.Empty;
        Input = string.Empty;
        ReplaceText = string.Empty;
        Output = string.Empty;
        MatchCount = 0;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(Output))
        {
            Clipboard.SetText(Output);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        Pattern = @"\b\w+@\w+\.\w+\b";
        Input = "Emails: john@example.com, jane@test.net, invalid-email";
        ReplaceText = "[EMAIL]";
        ReplaceMode = false;
        Test();
    }
}