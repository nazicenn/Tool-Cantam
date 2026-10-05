using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PingViewModel : ObservableObject
{
    private readonly IFileExportService _fileExportService;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _targetHost = "google.com";

    [ObservableProperty]
    private int _count = 4;

    [ObservableProperty]
    private int _timeoutMs = 2000;

    [ObservableProperty]
    private bool _isPinging;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private ObservableCollection<PingResult> _results = new();

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private int _sent;

    [ObservableProperty]
    private int _received;

    [ObservableProperty]
    private int _lost;

    [ObservableProperty]
    private int _minTime;

    [ObservableProperty]
    private int _maxTime;

    [ObservableProperty]
    private int _avgTime;

    public PingViewModel(IFileExportService fileExportService)
    {
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task StartPingAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetHost))
        {
            StatusText = "Please enter a hostname or IP address";
            return;
        }

        if (Count < 1 || Count > 50)
        {
            StatusText = "Count must be between 1 and 50";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsPinging = true;
        Results.Clear();
        Sent = 0;
        Received = 0;
        Lost = 0;
        MinTime = 0;
        MaxTime = 0;
        AvgTime = 0;
        SummaryText = string.Empty;
        StatusText = $"Pinging {TargetHost}...";

        var totalTime = 0L;
        var min = int.MaxValue;
        var max = 0;

        using var ping = new Ping();

        for (int i = 0; i < Count; i++)
        {
            if (_cts.Token.IsCancellationRequested)
                break;

            try
            {
                Sent++;
                var reply = await ping.SendPingAsync(TargetHost, TimeoutMs);

                var roundtrip = reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;

                var result = new PingResult
                {
                    Sequence = i + 1,
                    Status = reply.Status.ToString(),
                    RoundtripTime = roundtrip,
                    Address = reply.Address?.ToString() ?? "Unknown"
                };

                if (reply.Status == IPStatus.Success)
                {
                    Received++;
                    totalTime += roundtrip;
                    if (roundtrip < min) min = roundtrip;
                    if (roundtrip > max) max = roundtrip;
                }
                else
                {
                    Lost++;
                }

                Results.Add(result);
                StatusText = $"Ping {i + 1}/{Count} - {reply.Status}";
            }
            catch (Exception ex)
            {
                Results.Add(new PingResult
                {
                    Sequence = i + 1,
                    Status = $"Error: {ex.Message}",
                    RoundtripTime = -1,
                    Address = "Unknown"
                });
                Lost++;
            }
        }

        if (Received > 0)
        {
            MinTime = min;
            MaxTime = max;
            AvgTime = (int)(totalTime / Received);

            SummaryText = $"✓ Sent: {Sent} | Received: {Received} | Lost: {Lost} ({Lost * 100 / Sent}%)\n" +
                         $"✓ Min: {MinTime}ms | Max: {MaxTime}ms | Avg: {AvgTime}ms";
        }
        else
        {
            SummaryText = "✗ No successful pings. Host may be down or blocking ICMP.";
        }

        StatusText = "Ping completed";
        IsPinging = false;
    }

    [RelayCommand]
    private void CancelPing()
    {
        _cts?.Cancel();
        StatusText = "Cancelling...";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Results.Count == 0) return;

        var text = $"Ping Results for {TargetHost}\n" +
                   $"========================\n" +
                   $"{SummaryText}\n\n" +
                   $"Details:\n";

        foreach (var result in Results)
        {
            text += $"  {result.Sequence}: {result.Status} - {result.RoundtripTime}ms\n";
        }

        _fileExportService.CopyToClipboard(text);
        StatusText = "Results copied to clipboard";
    }

    [RelayCommand]
    private void Clear()
    {
        TargetHost = "google.com";
        Count = 4;
        TimeoutMs = 2000;
        Results.Clear();
        SummaryText = string.Empty;
        StatusText = "Ready.";
        Sent = Received = Lost = 0;
        MinTime = MaxTime = AvgTime = 0;
    }
}

public class PingResult
{
    public int Sequence { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RoundtripTime { get; set; }
    public string Address { get; set; } = string.Empty;
}