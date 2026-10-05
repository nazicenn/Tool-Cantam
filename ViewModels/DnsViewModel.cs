using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class DnsViewModel : ObservableObject
{
    private readonly IDnsService _dnsService;
    private readonly IFileExportService _fileExportService;

    [ObservableProperty]
    private string _targetDomain = string.Empty;

    [ObservableProperty]
    private bool _isQuerying;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private DnsRecord? _dnsRecord;

    [ObservableProperty]
    private ObservableCollection<string> _aRecords = new();

    [ObservableProperty]
    private ObservableCollection<string> _aaaaRecords = new();

    [ObservableProperty]
    private ObservableCollection<string> _mxRecords = new();

    [ObservableProperty]
    private ObservableCollection<string> _nsRecords = new();

    [ObservableProperty]
    private ObservableCollection<string> _txtRecords = new();

    [ObservableProperty]
    private string _soaRecord = string.Empty;

    public DnsViewModel(IDnsService dnsService, IFileExportService fileExportService)
    {
        _dnsService = dnsService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task QueryAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetDomain))
        {
            StatusText = "Please enter a domain";
            return;
        }

        if (!_dnsService.IsValidDomain(TargetDomain))
        {
            StatusText = "Invalid domain format";
            return;
        }

        IsQuerying = true;
        StatusText = "Querying DNS records...";
        ClearResults();

        try
        {
            var result = await _dnsService.LookupAsync(TargetDomain);
            DnsRecord = result;

            foreach (var a in result.A) ARecords.Add(a);
            foreach (var aaaa in result.AAAA) AaaaRecords.Add(aaaa);
            foreach (var mx in result.MX) MxRecords.Add(mx);
            foreach (var ns in result.NS) NsRecords.Add(ns);
            foreach (var txt in result.TXT) TxtRecords.Add(txt);
            SoaRecord = result.SOA;

            var total = result.A.Count + result.AAAA.Count + result.MX.Count +
                        result.NS.Count + result.TXT.Count;

            StatusText = total > 0
                ? $"Found {total} DNS records"
                : "No DNS records found";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsQuerying = false;
        }
    }

    [RelayCommand]
    private void CopyAll()
    {
        var allResults = GetFormattedResults();
        if (!string.IsNullOrWhiteSpace(allResults))
        {
            _fileExportService.CopyToClipboard(allResults);
            StatusText = "All results copied to clipboard";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TargetDomain = string.Empty;
        ClearResults();
        StatusText = "Ready.";
    }

    private void ClearResults()
    {
        ARecords.Clear();
        AaaaRecords.Clear();
        MxRecords.Clear();
        NsRecords.Clear();
        TxtRecords.Clear();
        SoaRecord = string.Empty;
        DnsRecord = null;
    }

    private string GetFormattedResults()
    {
        if (DnsRecord == null) return string.Empty;

        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"=== DNS Lookup for {DnsRecord.Domain} ===");
        lines.AppendLine();

        if (ARecords.Count > 0) lines.AppendLine($"A Records (IPv4):\n  {string.Join("\n  ", ARecords)}\n");
        if (AaaaRecords.Count > 0) lines.AppendLine($"AAAA Records (IPv6):\n  {string.Join("\n  ", AaaaRecords)}\n");
        if (MxRecords.Count > 0) lines.AppendLine($"MX Records (Mail):\n  {string.Join("\n  ", MxRecords)}\n");
        if (NsRecords.Count > 0) lines.AppendLine($"NS Records (Nameservers):\n  {string.Join("\n  ", NsRecords)}\n");
        if (TxtRecords.Count > 0) lines.AppendLine($"TXT Records:\n  {string.Join("\n  ", TxtRecords)}\n");
        if (!string.IsNullOrWhiteSpace(SoaRecord)) lines.AppendLine($"SOA Record:\n  {SoaRecord}");

        return lines.ToString();
    }
}