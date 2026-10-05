// ViewModels/TextDiffViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class TextDiffViewModel : ObservableObject
{
    [ObservableProperty]
    private string _leftText = string.Empty;

    [ObservableProperty]
    private string _rightText = string.Empty;

    [ObservableProperty]
    private string _diffResult = string.Empty;

    [ObservableProperty]
    private bool _ignoreCase = false;

    [ObservableProperty]
    private bool _ignoreWhitespace = false;

    [ObservableProperty]
    private int _addedLines = 0;

    [ObservableProperty]
    private int _removedLines = 0;

    [ObservableProperty]
    private int _changedLines = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Compare()
    {
        try
        {
            var left = LeftText ?? string.Empty;
            var right = RightText ?? string.Empty;

            if (IgnoreWhitespace)
            {
                left = System.Text.RegularExpressions.Regex.Replace(left, @"\s+", " ");
                right = System.Text.RegularExpressions.Regex.Replace(right, @"\s+", " ");
            }

            var leftLines = left.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var rightLines = right.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var comparison = IgnoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var result = new StringBuilder();
            int added = 0, removed = 0, changed = 0;
            int maxLines = Math.Max(leftLines.Length, rightLines.Length);

            for (int i = 0; i < maxLines; i++)
            {
                string leftLine = i < leftLines.Length ? leftLines[i] : string.Empty;
                string rightLine = i < rightLines.Length ? rightLines[i] : string.Empty;

                if (string.Equals(leftLine, rightLine, comparison))
                {
                    result.AppendLine($"  {leftLine}");
                }
                else if (string.IsNullOrEmpty(leftLine))
                {
                    result.AppendLine($"+ {rightLine}");
                    added++;
                }
                else if (string.IsNullOrEmpty(rightLine))
                {
                    result.AppendLine($"- {leftLine}");
                    removed++;
                }
                else
                {
                    result.AppendLine($"~ {leftLine} → {rightLine}");
                    changed++;
                }
            }

            DiffResult = result.ToString();
            AddedLines = added;
            RemovedLines = removed;
            ChangedLines = changed;
            StatusText = $"Added: {added}, Removed: {removed}, Changed: {changed}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        LeftText = string.Empty;
        RightText = string.Empty;
        DiffResult = string.Empty;
        AddedLines = 0;
        RemovedLines = 0;
        ChangedLines = 0;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void Swap()
    {
        (LeftText, RightText) = (RightText, LeftText);
        Compare();
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrWhiteSpace(DiffResult))
        {
            Clipboard.SetText(DiffResult);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        LeftText = "Merhaba dünya\nBugün hava çok güzel\nBu bir testtir";
        RightText = "Merhaba dünya\nBugün hava çok sıcak\nBu bir testtir\nYeni satır";
        Compare();
    }
}