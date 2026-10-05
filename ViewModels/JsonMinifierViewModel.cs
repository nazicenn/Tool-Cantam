// ViewModels/JsonMinifierViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class JsonMinifierViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputJson = string.Empty;

    [ObservableProperty]
    private string _outputJson = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Minify()
    {
        if (string.IsNullOrWhiteSpace(InputJson))
        {
            StatusText = "Please enter JSON to minify";
            OutputJson = string.Empty;
            return;
        }

        try
        {
            // Parse and re-serialize without indentation
            using var doc = JsonDocument.Parse(InputJson);
            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
            doc.WriteTo(writer);
            writer.Flush();

            OutputJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            StatusText = "✓ JSON minified successfully";
        }
        catch (JsonException ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputJson))
        {
            StatusText = "Please enter JSON to format";
            OutputJson = string.Empty;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(InputJson);
            using var stream = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
            doc.WriteTo(writer);
            writer.Flush();

            OutputJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            StatusText = "✓ JSON formatted successfully";
        }
        catch (JsonException ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputJson = string.Empty;
        OutputJson = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputJson))
        {
            Clipboard.SetText(OutputJson);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputJson = "{\n  \"name\": \"John Doe\",\n  \"age\": 30,\n  \"email\": \"john@example.com\",\n  \"address\": {\n    \"city\": \"Istanbul\",\n    \"country\": \"Turkey\"\n  },\n  \"hobbies\": [\n    \"coding\",\n    \"gaming\"\n  ]\n}";
        StatusText = "Sample loaded";
    }
}