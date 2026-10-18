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

public partial class NsLookupViewModel : ObservableObject
{
    private readonly INsLookupService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<NsLookupViewModel> _logger;

    [ObservableProperty] private string _domain = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> NameServers { get; } = new();

    public NsLookupViewModel(INsLookupService service, IFileExportService fe, ILocalizerService loc, ILogger<NsLookupViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.NsLookup.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.NsLookup.Limits", _service.Timeout.TotalSeconds);

    partial void OnDomainChanged(string value) => LookupCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLookup))]
    private async Task LookupAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.LookupAsync(Domain, ct);
            if (!r.IsSuccess) { ReportFailure(r, "lookup"); NameServers.Clear(); return; }
            NameServers.Clear();
            foreach (var ns in r.Value!.NameServers) NameServers.Add(ns);
            _logger.LogInformation("NS lookup: {Domain} → {Count} servers", r.Value.Domain, r.Value.NameServers.Count);
            SetStatus(r.Value.NameServers.Count > 0 ? NotificationType.Success : NotificationType.Warning,
                "L.NsLookup.Status.Found", r.Value.NameServers.Count, r.Value.Domain);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "NS lookup failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanLookup() => !string.IsNullOrWhiteSpace(Domain);

    [RelayCommand]
    private void CopyResults()
    {
        if (NameServers.Count == 0) { SetStatus(NotificationType.Warning, "L.NsLookup.Validation.NothingToCopy"); return; }
        var text = $"NS records for {Domain}:\n" + string.Join("\n", NameServers);
        try { _fileExportService.CopyToClipboard(text); SetStatus(NotificationType.Success, "L.NsLookup.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand] private void LoadSample() { Domain = "google.com"; LookupCommand.Execute(null); }
    [RelayCommand] private void Clear() { Domain = string.Empty; NameServers.Clear(); SetStatus(NotificationType.Info, "L.NsLookup.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("NsLookup {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}