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

public partial class CorsAnalyzerViewModel : ObservableObject
{
    private readonly ICorsAnalyzerService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CorsAnalyzerViewModel> _logger;

    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string _headers = string.Empty;
    [ObservableProperty] private bool _treatMissingAsError;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _summaryText = string.Empty;
    [ObservableProperty] private bool _isValid;
    [ObservableProperty] private bool _isFromUrl;

    public ObservableCollection<CorsHeaderRow> ParsedHeaders { get; } = new();
    public ObservableCollection<CorsIssueRow> Issues { get; } = new();

    public CorsAnalyzerViewModel(ICorsAnalyzerService service, IFileExportService fe, ILocalizerService loc, ILogger<CorsAnalyzerViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CorsAnalyzer.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.CorsAnalyzer.Limits", _service.MaxInputLength / 1024);

    partial void OnIsFromUrlChanged(bool value) => AnalyzeCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new CorsOptions(TreatMissingAsError);
            var r = IsFromUrl
                ? await _service.FetchAndAnalyzeAsync(Url, opts, ct)
                : await _service.AnalyzeAsync(Headers, opts, ct);

            if (!r.IsSuccess) { ReportFailure(r, "analyze"); ClearResults(); return; }
            var v = r.Value!;

            ParsedHeaders.Clear();
            foreach (var h in v.Headers) ParsedHeaders.Add(new CorsHeaderRow(h.Name, h.Value, h.Kind.ToString()));
            Issues.Clear();
            foreach (var i in v.Issues) Issues.Add(new CorsIssueRow(i.Code, i.Message, i.Severity.ToString()));

            IsValid = v.IsValid;
            SummaryText = _localizer.Format("L.CorsAnalyzer.Summary", v.Status, v.Issues.Count, v.Elapsed.TotalMilliseconds);

            _logger.LogInformation("CORS: {Status}, {Issues} issues, {Ms:0}ms",
                v.Status, v.Issues.Count, v.Elapsed.TotalMilliseconds);

            SetStatus(v.IsValid ? NotificationType.Success : NotificationType.Error,
                v.IsValid ? "L.CorsAnalyzer.Status.Valid" : "L.CorsAnalyzer.Status.Invalid",
                v.Issues.Count);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "CORS analyze failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanAnalyze() => IsFromUrl ? !string.IsNullOrWhiteSpace(Url) : !string.IsNullOrWhiteSpace(Headers);

    [RelayCommand] private void SwitchToUrl() { IsFromUrl = true; SetStatus(NotificationType.Info, "L.CorsAnalyzer.Status.ModeUrl"); }
    [RelayCommand] private void SwitchToHeaders() { IsFromUrl = false; SetStatus(NotificationType.Info, "L.CorsAnalyzer.Status.ModeHeaders"); }

    [RelayCommand]
    private void CopyHeaders()
    {
        if (ParsedHeaders.Count == 0) { SetStatus(NotificationType.Warning, "L.CorsAnalyzer.Validation.NothingToCopy"); return; }
        var sb = new StringBuilder();
        foreach (var h in ParsedHeaders) sb.AppendLine($"{h.Name}: {h.Value}");
        try { _fileExportService.CopyToClipboard(sb.ToString()); SetStatus(NotificationType.Success, "L.CorsAnalyzer.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveReport()
    {
        if (ParsedHeaders.Count == 0) { SetStatus(NotificationType.Warning, "L.CorsAnalyzer.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.CorsAnalyzer.Filter.Text"), ".txt", "cors-report");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder();
        sb.AppendLine($"URL: {(IsFromUrl ? Url : "(manual headers)")}");
        sb.AppendLine($"Valid: {IsValid}");
        sb.AppendLine($"Summary: {SummaryText}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine("HEADERS:");
        foreach (var h in ParsedHeaders) sb.AppendLine($"  {h.Name}: {h.Value}");
        sb.AppendLine();
        sb.AppendLine("ISSUES:");
        foreach (var i in Issues) sb.AppendLine($"  [{i.Severity}] {i.Code}: {i.Message}");
        try { File.WriteAllText(path, sb.ToString()); SetStatus(NotificationType.Success, "L.CorsAnalyzer.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void Clear() { Url = string.Empty; Headers = string.Empty; ClearResults(); SetStatus(NotificationType.Info, "L.CorsAnalyzer.Status.Cleared"); }

    private void ClearResults() { ParsedHeaders.Clear(); Issues.Clear(); IsValid = false; SummaryText = string.Empty; }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CorsAnalyzer {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record CorsHeaderRow(string Name, string Value, string Kind);
public sealed record CorsIssueRow(string Code, string Message, string Severity);