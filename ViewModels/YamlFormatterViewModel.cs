// ViewModels/YamlFormatterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace tool_cantam.ViewModels;

public partial class YamlFormatterViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputYaml = string.Empty;

    [ObservableProperty]
    private string _outputYaml = string.Empty;

    [ObservableProperty]
    private int _indentSize = 2;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Format()
    {
        if (string.IsNullOrWhiteSpace(InputYaml))
        {
            StatusText = "Please enter YAML to format";
            OutputYaml = string.Empty;
            return;
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlObject = deserializer.Deserialize<object>(InputYaml);

            var serializer = new SerializerBuilder()
                .WithIndentedSequences()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .Build();

            OutputYaml = serializer.Serialize(yamlObject);
            StatusText = "✓ YAML formatted successfully";
        }
        catch (Exception ex)
        {
            OutputYaml = string.Empty;
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Minify()
    {
        if (string.IsNullOrWhiteSpace(InputYaml))
        {
            StatusText = "Please enter YAML to minify";
            return;
        }

        try
        {
            var lines = InputYaml.Split('\n');
            var result = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    result.AppendLine(trimmed);
                }
            }
            OutputYaml = result.ToString();
            StatusText = "✓ YAML minified successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        InputYaml = string.Empty;
        OutputYaml = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputYaml))
        {
            Clipboard.SetText(OutputYaml);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputYaml = "name: John Doe\nage: 30\nemail: john@example.com\naddress:\n  city: Istanbul\n  country: Turkey\nhobbies:\n  - coding\n  - gaming\n  - reading";
        Format();
    }
}