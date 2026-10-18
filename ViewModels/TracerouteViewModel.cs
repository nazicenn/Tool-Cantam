using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class TracerouteViewModel : ObservableObject
{
    private readonly ITracerouteService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<TracerouteViewModel> _logger;

    [ObservableProperty] private string _host = "google.com";
    [ObservableProperty] private int _maxHops = 30;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<TraceHopRow> Hops { get; } = new();

    public TracerouteViewModel(ITracerouteService service, IFileExportService fe, ILocalizerService loc, ILogger<TracerouteViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.Traceroute.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.Traceroute.Limits", _service.MinHops, _service.MaxHopsLimit);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartTraceAsync(CancellationToken ct)
    {
        IsBusy = true;
        Hops.Clear();
        try
        {
            var progress = new Progress<TraceHop>(h =>
            {
                Hops.Add(new TraceHopRow(h.Ttl, h.Address ?? "*", h.RoundtripMs, h.Status));
                SetStatus(NotificationType.Info, "L.Traceroute.Status.Running", h.Ttl);
            });

            var r = await _service.TraceAsync(Host, MaxHops, progress, ct);
            if (!r.IsSuccess) { ReportFailure(r, "trace"); return; }
            SetStatus(r.Value!.ReachedTarget ? NotificationType.Success : NotificationType.Warning,
                r.Value.ReachedTarget ? "L.Traceroute.Status.Reached" : "L.Traceroute.Status.Incomplete",
                r.Value.Hops.Count, r.Value.Elapsed.TotalSeconds);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Traceroute failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyResults()
    {
        if (Hops.Count == 0) { SetStatus(NotificationType.Warning, "L.Traceroute.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder($"traceroute to {Host}\n\n");
        foreach (var h in Hops) sb.AppendLine($"{h.Ttl,3}  {h.Address,-40}  {h.RoundtripMs,5} ms  {h.Status}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.Traceroute.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveResults()
    {
        if (Hops.Count == 0) { SetStatus(NotificationType.Warning, "L.Traceroute.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.Traceroute.Filter.Text"), ".txt", "traceroute");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder($"traceroute to {Host}\n\n");
        foreach (var h in Hops) sb.AppendLine($"{h.Ttl,3}  {h.Address,-40}  {h.RoundtripMs,5} ms  {h.Status}");
        try { File.WriteAllText(path, sb.ToString()); SetStatus(NotificationType.Success, "L.Traceroute.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void Clear() { Host = string.Empty; Hops.Clear(); SetStatus(NotificationType.Info, "L.Traceroute.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("Traceroute {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record TraceHopRow(int Ttl, string Address, long RoundtripMs, string Status);