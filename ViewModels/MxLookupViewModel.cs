using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class MxLookupViewModel : ObservableObject
{
    private readonly IMxLookupService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<MxLookupViewModel> _logger;

    [ObservableProperty] private string _domain = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<MxRow> MxRecords { get; } = new();

    public MxLookupViewModel(IMxLookupService service, IFileExportService fe, ILocalizerService loc, ILogger<MxLookupViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.MxLookup.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.MxLookup.Limits", _service.Timeout.TotalSeconds);

    partial void OnDomainChanged(string value) => LookupCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLookup))]
    private async Task LookupAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.LookupAsync(Domain, ct);
            if (!r.IsSuccess) { ReportFailure(r, "lookup"); MxRecords.Clear(); return; }
            MxRecords.Clear();
            foreach (var m in r.Value!.Records)
                MxRecords.Add(new MxRow(m.Priority, m.Exchange));
            _logger.LogInformation("MX lookup: {Domain} → {Count} records", r.Value.Domain, r.Value.Records.Count);
            SetStatus(r.Value.Records.Count > 0 ? NotificationType.Success : NotificationType.Warning,
                "L.MxLookup.Status.Found", r.Value.Records.Count, r.Value.Domain);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "MX lookup failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanLookup() => !string.IsNullOrWhiteSpace(Domain);

    [RelayCommand]
    private void CopyResults()
    {
        if (MxRecords.Count == 0) { SetStatus(NotificationType.Warning, "L.MxLookup.Validation.NothingToCopy"); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"MX records for {Domain}:");
        foreach (var r in MxRecords) sb.AppendLine($"{r.Priority}\t{r.Exchange}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.MxLookup.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand] private void LoadSample() { Domain = "gmail.com"; LookupCommand.Execute(null); }
    [RelayCommand] private void Clear() { Domain = string.Empty; MxRecords.Clear(); SetStatus(NotificationType.Info, "L.MxLookup.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("MxLookup {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record MxRow(int Priority, string Exchange);