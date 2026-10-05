using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class BinaryEncoderViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public BinaryEncoderViewModel(IFileExportService fileExportService)
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
            var bytes = Encoding.UTF8.GetBytes(InputText);
            var binary = new StringBuilder();
            foreach (var b in bytes)
            {
                binary.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
            OutputText = binary.ToString();
            StatusText = "Encoded to binary successfully";
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
            StatusText = "Please enter binary to decode";
            OutputText = string.Empty;
            return;
        }

        try
        {
            var cleaned = InputText.Replace(" ", "").Replace("-", "");
            var byteList = new List<byte>();
            for (int i = 0; i < cleaned.Length; i += 8)
            {
                if (i + 8 <= cleaned.Length)
                {
                    var byteStr = cleaned.Substring(i, 8);
                    byteList.Add(Convert.ToByte(byteStr, 2));
                }
            }
            OutputText = Encoding.UTF8.GetString(byteList.ToArray());
            StatusText = "Decoded from binary successfully";
        }
        catch
        {
            OutputText = string.Empty;
            StatusText = "Invalid binary format";
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