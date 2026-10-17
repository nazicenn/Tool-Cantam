using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class MxLookupViewModel : ObservableObject
{
    private readonly IDnsService _dnsService;

    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<MxRecord> MxRecords { get; } = new();

    public MxLookupViewModel(IDnsService dnsService)
    {
        _dnsService = dnsService;
    }

    public class MxRecord
    {
        public int Priority { get; set; }
        public string Exchange { get; set; } = string.Empty;
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
        StatusText = $"Looking up MX records for {Domain}...";
        MxRecords.Clear();

        try
        {
            var result = await _dnsService.LookupAsync(Domain);

            if (!result.IsSuccess)
            {
                StatusText = $"✗ {result.ErrorCode}";
                return;
            }

            // Yeni: result.Records içinden MX tipi olanları filtrele
            var mxEntries = result.Value!.Records
                .Where(r => string.Equals(r.Type, "MX", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Priority ?? int.MaxValue)
                .ToList();

            foreach (var entry in mxEntries)
            {
                MxRecords.Add(new MxRecord
                {
                    Priority = entry.Priority ?? 0,
                    Exchange = entry.Value
                });
            }

            StatusText = MxRecords.Count > 0
                ? $"✓ Found {MxRecords.Count} MX records for {Domain}"
                : $"No MX records found for {Domain}";
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
        MxRecords.Clear();
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (MxRecords.Count == 0) return;

        var results = $"MX Records for {Domain}:\n";
        foreach (var record in MxRecords)
        {
            results += $"{record.Priority} {record.Exchange}\n";
        }
        System.Windows.Clipboard.SetText(results);
        StatusText = "Results copied!";
    }

    [RelayCommand]
    private void LoadSample()
    {
        Domain = "gmail.com";
        _ = LookupAsync();
    }
}