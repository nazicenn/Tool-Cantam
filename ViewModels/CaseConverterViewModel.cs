using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class CaseConverterViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public CaseConverterViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void ToUpper()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }
        OutputText = InputText.ToUpper();
        StatusText = "Converted to UPPER CASE";
    }

    [RelayCommand]
    private void ToLower()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }
        OutputText = InputText.ToLower();
        StatusText = "Converted to lower case";
    }

    [RelayCommand]
    private void ToTitleCase()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }
        var text = InputText.ToLower();
        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        OutputText = string.Join(" ", words);
        StatusText = "Converted to Title Case";
    }

    [RelayCommand]
    private void ToCamelCase()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }
        var words = InputText.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (i == 0)
                words[i] = words[i].ToLower();
            else
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }
        OutputText = string.Join("", words);
        StatusText = "Converted to camelCase";
    }

    [RelayCommand]
    private void ToSnakeCase()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            return;
        }
        OutputText = InputText.ToLower().Replace(' ', '_');
        StatusText = "Converted to snake_case";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            _fileExportService.CopyToClipboard(OutputText);
            StatusText = "Output copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        StatusText = "Cleared. Ready.";
    }
}