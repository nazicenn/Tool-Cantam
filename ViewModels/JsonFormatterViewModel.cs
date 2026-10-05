using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text.Json;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class JsonFormatterViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready.";

    public JsonFormatterViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter JSON data";
            OutputText = string.Empty;
            return;
        }

        try
        {
            var jsonDocument = JsonDocument.Parse(InputText);
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            jsonDocument.WriteTo(writer);
            writer.Flush();
            OutputText = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            StatusText = "JSON formatted successfully";
        }
        catch (JsonException ex)
        {
            OutputText = string.Empty;
            StatusText = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            OutputText = string.Empty;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Minify()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter JSON data";
            OutputText = string.Empty;
            return;
        }

        try
        {
            var jsonDocument = JsonDocument.Parse(InputText);
            OutputText = JsonSerializer.Serialize(jsonDocument.RootElement);
            StatusText = "JSON minified successfully";
        }
        catch (JsonException ex)
        {
            OutputText = string.Empty;
            StatusText = $"Invalid JSON: {ex.Message}";
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