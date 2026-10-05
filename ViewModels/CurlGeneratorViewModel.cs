// ViewModels/CurlGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using static QRCoder.PayloadGenerator.ShadowSocksConfig;

namespace tool_cantam.ViewModels;

public partial class CurlGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _url = "https://jsonplaceholder.typicode.com/posts/1";

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string _headers = "Content-Type: application/json";

    [ObservableProperty]
    private string _body = "{\n  \"title\": \"test\",\n  \"body\": \"content\",\n  \"userId\": 1\n}";

    [ObservableProperty]
    private string _curlCommand = string.Empty;

    [ObservableProperty]
    private bool _includeHeaders = true;

    [ObservableProperty]
    private bool _includeBody = true;

    [ObservableProperty]
    private bool _isBodyVisible = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    partial void OnMethodChanged(string value)
    {
        IsBodyVisible = value == "POST" || value == "PUT" || value == "PATCH";
    }

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusText = "Please enter a URL";
            return;
        }

        try
        {
            var curl = new StringBuilder();
            curl.Append($"curl -X {Method} ");

            // Add quotes around URL
            curl.Append($"\"{Url}\"");

            // Add headers
            if (IncludeHeaders && !string.IsNullOrWhiteSpace(Headers))
            {
                var lines = Headers.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(':');
                    if (parts.Length == 2)
                    {
                        curl.Append($" \\\n  -H \"{parts[0].Trim()}: {parts[1].Trim()}\"");
                    }
                }
            }

            // Add body
            if (IncludeBody && IsBodyVisible && !string.IsNullOrWhiteSpace(Body))
            {
                var escapedBody = Body.Replace("\"", "\\\"");
                curl.Append($" \\\n  -d \"{escapedBody}\"");
            }

            CurlCommand = curl.ToString();
            StatusText = "✓ cURL command generated";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyCurl()
    {
        if (!string.IsNullOrWhiteSpace(CurlCommand))
        {
            Clipboard.SetText(CurlCommand);
            StatusText = "cURL command copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Url = string.Empty;
        Headers = string.Empty;
        Body = string.Empty;
        CurlCommand = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void LoadSampleGet()
    {
        Url = "https://jsonplaceholder.typicode.com/posts/1";
        Method = "GET";
        Headers = "Content-Type: application/json";
        Body = "";
        Generate();
    }

    [RelayCommand]
    private void LoadSamplePost()
    {
        Url = "https://jsonplaceholder.typicode.com/posts";
        Method = "POST";
        Headers = "Content-Type: application/json";
        Body = "{\n  \"title\": \"Test\",\n  \"body\": \"Content\",\n  \"userId\": 1\n}";
        Generate();
    }
}