// ViewModels/GeoIpLookupViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class GeoIpLookupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _region = string.Empty;

    [ObservableProperty]
    private string _zipCode = string.Empty;

    [ObservableProperty]
    private string _latitude = string.Empty;

    [ObservableProperty]
    private string _longitude = string.Empty;

    [ObservableProperty]
    private string _isp = string.Empty;

    [ObservableProperty]
    private string _organization = string.Empty;

    [ObservableProperty]
    private string _timezone = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    private readonly HttpClient _httpClient = new HttpClient();

    [RelayCommand]
    private async Task LookupAsync()
    {
        if (string.IsNullOrWhiteSpace(IpAddress))
        {
            StatusText = "Please enter an IP address";
            return;
        }

        IsLoading = true;
        StatusText = $"Looking up {IpAddress}...";

        try
        {
            string ip = IpAddress.Trim();
            var url = $"https://ip-api.com/json/{ip}?fields=status,message,country,city,regionName,zip,lat,lon,isp,org,timezone";
            var response = await _httpClient.GetStringAsync(url);
            var json = JsonSerializer.Deserialize<JsonElement>(response);

            if (json.GetProperty("status").GetString() == "success")
            {
                Country = json.GetProperty("country").GetString() ?? "Unknown";
                City = json.GetProperty("city").GetString() ?? "Unknown";
                Region = json.GetProperty("regionName").GetString() ?? "Unknown";
                ZipCode = json.GetProperty("zip").GetString() ?? "Unknown";
                Latitude = json.GetProperty("lat").GetDouble().ToString("F4");
                Longitude = json.GetProperty("lon").GetDouble().ToString("F4");
                Isp = json.GetProperty("isp").GetString() ?? "Unknown";
                Organization = json.GetProperty("org").GetString() ?? "Unknown";
                Timezone = json.GetProperty("timezone").GetString() ?? "Unknown";

                StatusText = $"✓ Location found for {IpAddress}";
            }
            else
            {
                var message = json.GetProperty("message").GetString();
                StatusText = $"✗ {message}";
                ClearResults();
            }
        }
        catch (HttpRequestException)
        {
            StatusText = "✗ Network error. Please check your connection.";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LookupMyIpAsync()
    {
        IsLoading = true;
        StatusText = "Looking up your IP...";

        try
        {
            var response = await _httpClient.GetStringAsync("https://api.ipify.org");
            IpAddress = response.Trim();
            await LookupAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        IpAddress = string.Empty;
        ClearResults();
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResults()
    {
        var results = $"IP: {IpAddress}\n" +
                      $"Country: {Country}\n" +
                      $"City: {City}\n" +
                      $"Region: {Region}\n" +
                      $"ZIP: {ZipCode}\n" +
                      $"Coordinates: {Latitude}, {Longitude}\n" +
                      $"ISP: {Isp}\n" +
                      $"Organization: {Organization}\n" +
                      $"Timezone: {Timezone}";
        Clipboard.SetText(results);
        StatusText = "Results copied to clipboard!";
    }

    private void ClearResults()
    {
        Country = string.Empty;
        City = string.Empty;
        Region = string.Empty;
        ZipCode = string.Empty;
        Latitude = string.Empty;
        Longitude = string.Empty;
        Isp = string.Empty;
        Organization = string.Empty;
        Timezone = string.Empty;
    }
}