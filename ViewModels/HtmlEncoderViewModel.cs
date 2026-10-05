using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Web;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HtmlEncoderViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public HtmlEncoderViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Encode()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to encode";
            OutputText = string.Empty;
            return;
        }

        try
        {
            OutputText = HttpUtility.HtmlEncode(InputText);
            StatusText = "Encoded successfully";
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Decode()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to decode";
            OutputText = string.Empty;
            return;
        }

        try
        {
            OutputText = HttpUtility.HtmlDecode(InputText);
            StatusText = "Decoded successfully";
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