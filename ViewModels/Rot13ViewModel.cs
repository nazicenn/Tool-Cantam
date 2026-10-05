using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class Rot13ViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public Rot13ViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Transform()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to transform";
            OutputText = string.Empty;
            return;
        }

        try
        {
            char[] chars = InputText.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= 'a' && chars[i] <= 'z')
                {
                    chars[i] = (char)(((chars[i] - 'a' + 13) % 26) + 'a');
                }
                else if (chars[i] >= 'A' && chars[i] <= 'Z')
                {
                    chars[i] = (char)(((chars[i] - 'A' + 13) % 26) + 'A');
                }
            }
            OutputText = new string(chars);
            StatusText = "ROT13 transformation completed";
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
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