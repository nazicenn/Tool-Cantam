// ViewModels/RestApiClientViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using static QRCoder.PayloadGenerator.ShadowSocksConfig;

namespace tool_cantam.ViewModels;

public partial class RestApiClientViewModel : ObservableObject
{
    [ObservableProperty]
    private string _url = "https://jsonplaceholder.typicode.com/posts";

    [ObservableProperty]
    private string _method = "GET";

    [ObservableProperty]
    private string _headers = "Content-Type: application/json";

    [ObservableProperty]
    private string _body = "{\n  \"title\": \"test\",\n  \"body\": \"content\",\n  \"userId\": 1\n}";

    [ObservableProperty]
    private string _response = string.Empty;

    [ObservableProperty]
    private string _responseHeaders = string.Empty;

    [ObservableProperty]
    private int _statusCode = 0;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private long _responseTime = 0;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isBodyVisible = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    private readonly HttpClient _httpClient = new HttpClient();

    partial void OnMethodChanged(string value)
    {
        IsBodyVisible = value == "POST" || value == "PUT" || value == "PATCH";
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "Please enter a URL";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Sending {Method} request...";

        try
        {
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(Method),
                RequestUri = new Uri(Url)
            };

            if (!string.IsNullOrWhiteSpace(Headers))
            {
                var lines = Headers.Split('\n');
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(':');
                    if (parts.Length == 2)
                    {
                        request.Headers.TryAddWithoutValidation(parts[0].Trim(), parts[1].Trim());
                    }
                }
            }

            if (IsBodyVisible && !string.IsNullOrWhiteSpace(Body))
            {
                request.Content = new StringContent(Body, Encoding.UTF8, "application/json");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            ResponseTime = stopwatch.ElapsedMilliseconds;
            StatusCode = (int)response.StatusCode;
            StatusText = response.StatusCode.ToString();

            var headers = new StringBuilder();
            foreach (var header in response.Headers)
            {
                headers.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            ResponseHeaders = headers.ToString();

            var responseBody = await response.Content.ReadAsStringAsync();
            Response = FormatJson(responseBody);

            StatusMessage = $"✓ {Method} completed in {ResponseTime}ms";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = $"✗ Network error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string FormatJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            var obj = JsonSerializer.Deserialize<object>(json);
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Response = string.Empty;
        ResponseHeaders = string.Empty;
        StatusCode = 0;
        StatusText = string.Empty;
        ResponseTime = 0;
        StatusMessage = "Cleared";
    }

    [RelayCommand]
    private void CopyResponse()
    {
        if (!string.IsNullOrWhiteSpace(Response))
        {
            Clipboard.SetText(Response);
            StatusMessage = "Response copied!";
        }
    }

    [RelayCommand]
    private void CopyCurl()
    {
        var curl = $"curl -X {Method} '{Url}'";
        if (!string.IsNullOrWhiteSpace(Headers))
        {
            var lines = Headers.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(':');
                if (parts.Length == 2)
                {
                    curl += $" -H '{parts[0].Trim()}: {parts[1].Trim()}'";
                }
            }
        }
        if (IsBodyVisible && !string.IsNullOrWhiteSpace(Body))
        {
            curl += $" -d '{Body.Replace("\n", " ").Replace("\"", "\\\"")}'";
        }
        Clipboard.SetText(curl);
        StatusMessage = "cURL command copied!";
    }

    [RelayCommand]
    private void LoadSampleGet()
    {
        Url = "https://jsonplaceholder.typicode.com/posts/1";
        Method = "GET";
        Headers = "Content-Type: application/json";
        Body = "";
        StatusMessage = "GET sample loaded";
    }

    [RelayCommand]
    private void LoadSamplePost()
    {
        Url = "https://jsonplaceholder.typicode.com/posts";
        Method = "POST";
        Headers = "Content-Type: application/json";
        Body = "{\n  \"title\": \"Test Post\",\n  \"body\": \"Content here\",\n  \"userId\": 1\n}";
        StatusMessage = "POST sample loaded";
    }
}