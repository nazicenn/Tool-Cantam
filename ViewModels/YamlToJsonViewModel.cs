// ViewModels/YamlToJsonViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.Json;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace tool_cantam.ViewModels;

public partial class YamlToJsonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputYaml = string.Empty;

    [ObservableProperty]
    private string _outputJson = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(InputYaml))
        {
            StatusText = "Please enter YAML to convert";
            OutputJson = string.Empty;
            return;
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlObject = deserializer.Deserialize<object>(InputYaml);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(yamlObject, jsonOptions);

            OutputJson = json;
            StatusText = "✓ Successfully converted YAML to JSON";
        }
        catch (Exception ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Conversion error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputYaml = string.Empty;
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
    private void Swap()
    {
        (InputYaml, OutputJson) = (OutputJson, InputYaml);
        StatusText = "Swapped";
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputYaml = "name: John Doe\nage: 30\nemail: john@example.com\naddress:\n  city: Istanbul\n  country: Turkey\nhobbies:\n  - coding\n  - gaming";
        Convert();
    }
}