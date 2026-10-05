// ViewModels/CsvToJsonViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace tool_cantam.ViewModels;

public partial class CsvToJsonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputCsv = string.Empty;

    [ObservableProperty]
    private string _outputJson = string.Empty;

    [ObservableProperty]
    private string _separator = ",";

    [ObservableProperty]
    private bool _firstRowAsHeader = true;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Convert()
    {
        if (string.IsNullOrWhiteSpace(InputCsv))
        {
            StatusText = "Please enter CSV to convert";
            OutputJson = string.Empty;
            return;
        }

        try
        {
            var lines = InputCsv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
            {
                StatusText = "No data to convert";
                return;
            }

            string[]? headers = null;
            int startLine = 0;

            if (FirstRowAsHeader && lines.Length > 0)
            {
                headers = ParseCsvLine(lines[0], Separator);
                startLine = 1;
            }

            var jsonArray = new StringBuilder();
            jsonArray.AppendLine("[");

            for (int i = startLine; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i], Separator);
                var jsonObject = new StringBuilder();
                jsonObject.Append("{");

                for (int j = 0; j < values.Length; j++)
                {
                    string key = (headers != null && j < headers.Length) ? headers[j] : $"Column{j + 1}";
                    string value = EscapeJson(values[j]);

                    jsonObject.Append($"\"{key}\": \"{value}\"");
                    if (j < values.Length - 1)
                        jsonObject.Append(",");
                }

                jsonObject.Append("}");
                jsonArray.Append("  " + jsonObject);
                if (i < lines.Length - 1)
                    jsonArray.AppendLine(",");
                else
                    jsonArray.AppendLine();
            }

            jsonArray.Append("]");
            OutputJson = jsonArray.ToString();
            StatusText = $"✓ Converted {lines.Length - startLine} rows to JSON";
        }
        catch (Exception ex)
        {
            OutputJson = string.Empty;
            StatusText = $"✗ Conversion error: {ex.Message}";
        }
    }

    private string[] ParseCsvLine(string line, string separator)
    {
        if (string.IsNullOrEmpty(line))
            return Array.Empty<string>();

        var result = new System.Collections.Generic.List<string>();
        var inQuote = false;
        var currentField = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuote = !inQuote;
            }
            else if (c == separator[0] && !inQuote)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result.ToArray();
    }

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
    }

    [RelayCommand]
    private void Clear()
    {
        InputCsv = string.Empty;
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
        InputCsv = "name,age,city\nAhmet,30,İstanbul\nMehmet,25,Ankara\nAyşe,28,İzmir";
        Convert();
    }
}