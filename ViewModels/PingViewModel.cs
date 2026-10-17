using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Arayüzde gösterilecek ping satırı.</summary>
public sealed record PingRow(int Sequence, string Status, string RttText, string Address, bool IsSuccess);

public partial class PingViewModel : ObservableObject
{
    private readonly IPingService _pingService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<PingViewModel> _logger;

    [ObservableProperty] private string _targetHost = "google.com";
    [ObservableProperty] private int _count = 4;
    [ObservableProperty] private int _timeoutMs = 2000;
    [ObservableProperty] private bool _isPinging;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private double _progressPercent;

    [ObservableProperty] private int _sent;
    [ObservableProperty] private int _received;
    [ObservableProperty] private int _lost;
    [ObservableProperty] private int _packetLoss;
    [ObservableProperty] private string _minText = "-";
    [ObservableProperty] private string _maxText = "-";
    [ObservableProperty] private string _avgText = "-";

    public ObservableCollection<PingRow> Results { get; } = new();

    public event Action? RequestTargetFocus;

    public PingViewModel(
        IPingService pingService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<PingViewModel> logger)
    {
        _pingService = pingService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Ping.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.Ping.Limits", _pingService.MaxCount, _pingService.MinTimeoutMs, _pingService.MaxTimeoutMs);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task PingAsync(CancellationToken cancellationToken)
    {
        string target = TargetHost?.Trim() ?? string.Empty;

        if (target.Length == 0)
        {
            RequestTargetFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Ping.Validation.InvalidTarget");
            return;
        }

        IsPinging = true;
        Results.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        Sent = Received = Lost = PacketLoss = 0;
        MinText = MaxText = AvgText = "-";
        ProgressPercent = 0;

        var progress = new Progress<PingProgress>(p =>
        {
            ProgressPercent = p.Total > 0 ? p.Completed * 100.0 / p.Total : 0;
        });

        try
        {
            SetStatus(NotificationType.Info, "L.Ping.Status.Pinging", target, Count);

            ToolResult<PingReport> result = await _pingService.PingAsync(
                target, Count, TimeoutMs, progress, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "ping");
                return;
            }

            PingReport report = result.Value!;

            foreach (var attempt in report.Attempts)
            {
                bool ok = attempt.RoundtripMs.HasValue;
                Results.Add(new PingRow(
                    attempt.Sequence,
                    attempt.Status,
                    ok ? $"{attempt.RoundtripMs} ms" : "-",
                    attempt.Address,
                    ok));
            }

            Sent = report.Sent;
            Received = report.Received;
            Lost = report.Lost;
            PacketLoss = report.PacketLossPercent;
            MinText = report.MinMs.HasValue ? $"{report.MinMs} ms" : "-";
            MaxText = report.MaxMs.HasValue ? $"{report.MaxMs} ms" : "-";
            AvgText = report.AvgMs.HasValue ? $"{report.AvgMs} ms" : "-";
            HasResults = true;
            ProgressPercent = 100;

            SummaryText = _localizer.Format("L.Ping.Status.Summary",
                report.ResolvedAddress, report.Sent, report.Received, report.Lost,
                report.PacketLossPercent,
                report.MinMs ?? 0, report.MaxMs ?? 0, report.AvgMs ?? 0,
                report.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "Ping finished: target={Target} address={Address} sent={Sent} received={Received} lost={Lost} elapsedMs={Elapsed}",
                report.Target, report.ResolvedAddress, report.Sent, report.Received, report.Lost,
                report.Elapsed.TotalMilliseconds);

            if (report.Received == 0)
            {
                bool allBlocked = report.Attempts.All(a => a.Status == "ICMP Blocked");
                if (allBlocked)
                {
                    SetStatus(NotificationType.Error, "L.Ping.Error.IcmpBlocked");
                }
                else
                {
                    SetStatus(NotificationType.Warning, "L.Ping.Status.NoReply", report.Sent);
                }
                return;
            }

            SetStatus(NotificationType.Success, "L.Ping.Status.Finished", report.Received, report.Sent);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ping cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsPinging = false;
        }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Results.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Ping.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(BuildReport());
            SetStatus(NotificationType.Success, "L.Ping.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping result could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveResultAsync()
    {
        if (Results.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.Ping.Validation.NothingToSave");
            return;
        }

        IsPinging = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                BuildReport(),
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "ping-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Ping.Status.Saved" : "L.Ping.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping result could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsPinging = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        if (PingCancelCommand is not null && PingCancelCommand.CanExecute(null))
            PingCancelCommand.Execute(null);

        Results.Clear();
        HasResults = false;
        SummaryText = string.Empty;
        Sent = Received = Lost = PacketLoss = 0;
        MinText = MaxText = AvgText = "-";
        ProgressPercent = 0;
        SetStatus(NotificationType.Info, "L.Ping.Status.Cleared");
    }

    private string BuildReport()
    {
        var b = new System.Text.StringBuilder();
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Target")}: {TargetHost}");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Sent")}: {Sent}");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Received")}: {Received}");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Lost")}: {Lost} ({PacketLoss}%)");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Min")}: {MinText}");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Max")}: {MaxText}");
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Avg")}: {AvgText}");
        b.AppendLine();
        b.AppendLine($"{_localizer.Get("L.Ping.Column.Sequence")}\t{_localizer.Get("L.Ping.Column.Status")}\t{_localizer.Get("L.Ping.Column.Rtt")}\t{_localizer.Get("L.Ping.Column.Address")}");

        foreach (var row in Results)
            b.AppendLine($"{row.Sequence}\t{row.Status}\t{row.RttText}\t{row.Address}");

        return b.ToString().TrimEnd();
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("Ping {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Ping {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}