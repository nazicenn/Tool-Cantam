using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class HttpHeaderService : IHttpHeaderService
{
    private readonly HttpClient _httpClient = new();

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public async Task<Dictionary<string, string>> GetHeadersAsync(string url)
    {
        var headers = new Dictionary<string, string>();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);

            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            headers["Status Code"] = ((int)response.StatusCode).ToString();
            headers["Status Description"] = response.ReasonPhrase ?? "Unknown";
        }
        catch (HttpRequestException ex)
        {
            headers["Error"] = $"Request failed: {ex.Message}";
        }
        catch (Exception ex)
        {
            headers["Error"] = ex.Message;
        }

        return headers;
    }
}