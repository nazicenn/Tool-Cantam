// ViewModels/TracerouteViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class TracerouteViewModel : ObservableObject
{
    [ObservableProperty]
    private string _host = "google.com";

    [ObservableProperty]
    private int _maxHops = 30;

    [ObservableProperty]
    private bool _isRunning = false;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<TraceHop> Hops { get; } = new();

    public class TraceHop
    {
        public int Hop { get; set; }
        public string Ip { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public long Time1 { get; set; }
        public long Time2 { get; set; }
        public long Time3 { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    [RelayCommand]
    private async Task StartTraceAsync()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            StatusText = "Please enter a host";
            return;
        }

        IsRunning = true;
        StatusText = $"Tracing route to {Host}...";
        Hops.Clear();

        await Task.Run(() =>
        {
            for (int ttl = 1; ttl <= MaxHops; ttl++)
            {
                var hop = new TraceHop { Hop = ttl };

                try
                {
                    var ping = new Ping();
                    var options = new PingOptions(ttl, true);
                    var reply = ping.Send(Host, 5000, new byte[32], options);

                    if (reply.Status == IPStatus.Success)
                    {
                        hop.Ip = reply.Address?.ToString() ?? "???";
                        hop.Time1 = reply.RoundtripTime;
                        hop.Time2 = reply.RoundtripTime;
                        hop.Time3 = reply.RoundtripTime;
                        hop.Status = "Success";

                        Application.Current.Dispatcher.Invoke(() => Hops.Add(hop));

                        if (reply.Address?.ToString() == Host || reply.Address?.ToString() == "???")
                        {
                            StatusText = $"Trace complete to {Host}";
                            break;
                        }
                    }
                    else
                    {
                        hop.Ip = "*";
                        hop.Status = "Timeout";
                        Application.Current.Dispatcher.Invoke(() => Hops.Add(hop));
                    }
                }
                catch
                {
                    hop.Ip = "*";
                    hop.Status = "Error";
                    Application.Current.Dispatcher.Invoke(() => Hops.Add(hop));
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText = $"Tracing... {ttl}/{MaxHops} hops";
                });
            }
        });

        IsRunning = false;
        StatusText = $"✓ Traceroute completed for {Host}";
    }

    [RelayCommand]
    private void Clear()
    {
        Host = "google.com";
        Hops.Clear();
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Hops.Count == 0) return;

        var results = $"Traceroute to {Host}\n\n";
        foreach (var hop in Hops)
        {
            results += $"{hop.Hop}\t{hop.Ip}\t{hop.Time1}ms\t{hop.Time2}ms\t{hop.Time3}ms\n";
        }
        Clipboard.SetText(results);
        StatusText = "Results copied!";
    }
}