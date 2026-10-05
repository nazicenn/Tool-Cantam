using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Text.RegularExpressions;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class UnicodeConverterViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public UnicodeConverterViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void ToUnicode()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to convert";
            OutputText = string.Empty;
            return;
        }

        try
        {
            var unicode = new StringBuilder();
            foreach (char c in InputText)
            {
                unicode.Append($"\\u{(int)c:X4}");
            }
            OutputText = unicode.ToString();
            StatusText = "Converted to Unicode escape sequences";
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void FromUnicode()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter Unicode escape sequences (e.g., \\u0041)";
            OutputText = string.Empty;
            return;
        }

        try
        {
            var regex = new Regex(@"\\u([0-9A-Fa-f]{4})");
            var result = regex.Replace(InputText, match =>
            {
                int code = Convert.ToInt32(match.Groups[1].Value, 16);
                return ((char)code).ToString();
            });
            OutputText = result;
            StatusText = "Converted from Unicode escape sequences";
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        string temp = InputText;
        InputText = OutputText;
        OutputText = temp;
        StatusText = "Swapped input and output";
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