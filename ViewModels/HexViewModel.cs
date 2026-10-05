using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class HexViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public HexViewModel(IFileExportService fileExportService)
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
            byte[] bytes = Encoding.UTF8.GetBytes(InputText);
            OutputText = Convert.ToHexString(bytes);
            StatusText = "Encoded to hex successfully";
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
            StatusText = "Please enter hex to decode";
            OutputText = string.Empty;
            return;
        }

        try
        {
            string cleaned = InputText.Replace(" ", "").Replace("-", "");
            byte[] bytes = new byte[cleaned.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);
            }
            OutputText = Encoding.UTF8.GetString(bytes);
            StatusText = "Decoded from hex successfully";
        }
        catch
        {
            OutputText = string.Empty;
            StatusText = "Invalid hex format";
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