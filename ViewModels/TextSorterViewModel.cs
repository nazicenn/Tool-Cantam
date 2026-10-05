// ViewModels/TextSorterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class TextSorterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private bool _ascending = true;

    [ObservableProperty]
    private bool _ignoreCase = true;

    [ObservableProperty]
    private bool _removeEmptyLines = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Sort()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "No text to sort";
            return;
        }

        try
        {
            var lines = InputText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (RemoveEmptyLines)
                lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            var comparison = IgnoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            var sorted = Ascending
                ? lines.OrderBy(l => l, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, IgnoreCase))
                : lines.OrderByDescending(l => l, StringComparer.Create(System.Globalization.CultureInfo.CurrentCulture, IgnoreCase));

            OutputText = string.Join(Environment.NewLine, sorted);
            StatusText = $"Sorted {lines.Length} lines";
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
        OutputText = string.Empty;
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
}