using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PortScannerViewModel : ObservableObject
{
    private readonly IPortScannerService _portScannerService;
    private readonly IFileExportService _fileExportService;
    private CancellationTokenSource? _scanCts;
    private readonly Stopwatch _stopwatch = new();

    [ObservableProperty] private string _targetHost = "127.0.0.1";
    [ObservableProperty] private int _startPort = 1;
    [ObservableProperty] private int _endPort = 1024;
    [ObservableProperty] private string _scanStatus = "Ready. Awaiting scan...";
    [ObservableProperty] private bool _isScanning = false;
    [ObservableProperty] private string _elapsedTime = string.Empty;

    public ObservableCollection<int> OpenPorts { get; } = new();

    public PortScannerViewModel(IPortScannerService portScannerService, IFileExportService fileExportService)
    {
        _portScannerService = portScannerService;
        _fileExportService = fileExportService;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning) return;

        if (string.IsNullOrWhiteSpace(TargetHost))
        {
            MessageBox.Show("Please enter a valid IP address or hostname to scan.", "Missing Host", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsScanning = true;
        OpenPorts.Clear();
        ElapsedTime = string.Empty;

        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        _stopwatch.Restart();

        try
        {
            ScanStatus = $"Scanning {TargetHost} : {StartPort}-{EndPort} in progress...";

            var results = await _portScannerService.ScanPortsAsync(
                TargetHost, StartPort, EndPort, timeoutMs: 1000, cancellationToken: _scanCts.Token);

            foreach (var port in results)
                OpenPorts.Add(port);

            _stopwatch.Stop();
            ElapsedTime = FormatElapsed(_stopwatch.Elapsed);
            ScanStatus = $"Scan completed. {OpenPorts.Count} open port(s) found.";
        }
        catch (OperationCanceledException)
        {
            _stopwatch.Stop();
            ElapsedTime = FormatElapsed(_stopwatch.Elapsed);
            ScanStatus = $"Scan aborted. {OpenPorts.Count} port(s) found before cancellation.";
        }
        catch (ArgumentException ex)
        {
            _stopwatch.Stop();
            ElapsedTime = FormatElapsed(_stopwatch.Elapsed);
            ScanStatus = ex.Message;
        }
        catch (Exception ex)
        {
            _stopwatch.Stop();
            ElapsedTime = FormatElapsed(_stopwatch.Elapsed);
            ScanStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        if (_scanCts is { IsCancellationRequested: false })
        {
            _scanCts.Cancel();
            ScanStatus = "Cancelling...";
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        if (OpenPorts.Count == 0)
        {
            MessageBox.Show("No open ports found to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var reportContent = $"Port Scan Report for {TargetHost}\n========================\n"
                            + string.Join(Environment.NewLine, OpenPorts);

        await _fileExportService.ExportTextAsync(reportContent, "Log Files|*.log|Text Files|*.txt|All Files|*.*", ".log", "ScanReport");
    }

    private static string FormatElapsed(TimeSpan ts)
    {
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}.{ts.Milliseconds / 100}s";
        return $"{ts.TotalSeconds:F2}s";
    }
}