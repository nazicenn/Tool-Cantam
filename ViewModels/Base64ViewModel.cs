using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class Base64ViewModel : ObservableObject
{
    private readonly IBase64Service _base64Service;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileStatus = string.Empty;

    public event Action? RequestInputFocus;

    public Base64ViewModel(IBase64Service base64Service, IFileExportService fileExportService)
    {
        _base64Service = base64Service;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void EncodeText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            MessageBox.Show("Please enter text to encode.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            RequestInputFocus?.Invoke();
            return;
        }

        try
        {
            OutputText = _base64Service.EncodeText(InputText);
            InputText = string.Empty;
            FileStatus = "Encoded successfully.";
        }
        catch (Exception ex)
        {
            OutputText = $"[ERROR] {ex.Message}";
            FileStatus = "Encoding failed.";
        }
    }

    [RelayCommand]
    private void DecodeText()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            MessageBox.Show("Please enter text to decode.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            RequestInputFocus?.Invoke();
            return;
        }

        try
        {
            OutputText = _base64Service.DecodeText(InputText);
            FileStatus = "Decoded successfully.";
        }
        catch (Exception ex)
        {
            OutputText = $"[ERROR] {ex.Message}";
            FileStatus = "Decoding failed.";
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            _fileExportService.CopyToClipboard(OutputText);
            FileStatus = "Output copied to clipboard.";
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText)) return;

        bool success = await _fileExportService.ExportTextAsync(OutputText, "Text Files (*.txt)|*.txt|All Files (*.*)|*.*", ".txt");
        if (success)
        {
            FileStatus = "Output saved to file.";
        }
    }

    [RelayCommand]
    private async Task EncodeFileAsync()
    {
        string sourceFile = FilePath;
        if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
        {
            var selectedPath = _fileExportService.RequestOpenPath("All Files (*.*)|*.*", "Select Source File");
            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            sourceFile = selectedPath;
            FilePath = sourceFile;
        }

        var savePath = _fileExportService.RequestSavePath("Base64 Files (*.b64)|*.b64|All Files (*.*)|*.*", ".b64");
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                FileStatus = "Encoding Stream...";
                bool success = await _base64Service.EncodeFileAsync(sourceFile, savePath);
                FileStatus = success ? "File Successfully Encoded" : "Encoding Failed";
            }
            catch (Exception ex)
            {
                FileStatus = $"[ERROR] {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task DecodeFileAsync()
    {
        string sourceFile = FilePath;
        if (string.IsNullOrWhiteSpace(sourceFile) || !File.Exists(sourceFile))
        {
            var selectedPath = _fileExportService.RequestOpenPath("All Files (*.*)|*.*", "Select Base64 File");
            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            sourceFile = selectedPath;
            FilePath = sourceFile;
        }

        var savePath = _fileExportService.RequestSavePath("All Files (*.*)|*.*", "");
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                FileStatus = "Decoding Stream...";
                bool success = await _base64Service.DecodeFileAsync(sourceFile, savePath);
                FileStatus = success ? "Decoded and Saved" : "Decoding Failed";
            }
            catch (Exception ex)
            {
                FileStatus = $"[ERROR] {ex.Message}";
            }
        }
    }
}