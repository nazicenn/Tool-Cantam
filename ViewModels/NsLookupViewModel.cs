using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class NsLookupViewModel : ObservableObject
{
    private readonly IDnsService _dnsService;

    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<string> NameServers { get; } = new();

    public NsLookupViewModel(IDnsService dnsService)
    {
        _dnsService = dnsService;
    }

    [RelayCommand]
    private async Task LookupAsync()
    {
        if (string.IsNullOrWhiteSpace(Domain))
        {
            StatusText = "Please enter a domain";
            return;
        }

        IsLoading = true;
        StatusText = $"Looking up NS records for {Domain}...";
        NameServers.Clear();

        try
        {
            var result = await _dnsService.LookupAsync(Domain);

            foreach (var ns in result.NS)
                NameServers.Add(ns);

            StatusText = NameServers.Count > 0
                ? $"✓ Found {NameServers.Count} name servers for {Domain}"
                : $"No NS records found for {Domain}";
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
        Domain = string.Empty;
        NameServers.Clear();
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (NameServers.Count == 0) return;

        var results = $"Name Servers for {Domain}:\n";
        foreach (var ns in NameServers)
        {
            results += $"{ns}\n";
        }
        Clipboard.SetText(results);
        StatusText = "Results copied!";
    }

    [RelayCommand]
    private void LoadSample()
    {
        Domain = "google.com";
        _ = LookupAsync();
    }
}