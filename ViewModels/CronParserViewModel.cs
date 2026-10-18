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

public partial class CronParserViewModel : ObservableObject
{
    private readonly ICronParserService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CronParserViewModel> _logger;

    [ObservableProperty] private string _cronExpression = "0 0 * * *";
    [ObservableProperty] private string _result = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<CronFieldRow> Fields { get; } = new();
    public ObservableCollection<string> NextRuns { get; } = new();

    public CronParserViewModel(ICronParserService service, IFileExportService fe, ILocalizerService loc, ILogger<CronParserViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CronParser.Status.Ready");
    }

    public string LimitsText => _localizer.Get("L.CronParser.Limits");

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanParse))]
    private async Task ParseAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ParseAsync(CronExpression, ct);
            if (!r.IsSuccess) { ReportFailure(r, "parse"); Result = string.Empty; Fields.Clear(); NextRuns.Clear(); return; }
            var v = r.Value!;
            Fields.Clear();
            foreach (var f in v.Fields) Fields.Add(new CronFieldRow(f.Name, f.Raw, f.Description));
            NextRuns.Clear();
            foreach (var dt in v.NextRuns) NextRuns.Add(dt.ToString("yyyy-MM-dd HH:mm"));
            Result = v.Summary;
            _logger.LogInformation("Cron parsed: {Expr} → {Summary}", v.Expression, v.Summary);
            SetStatus(NotificationType.Success, "L.CronParser.Status.Parsed");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Cron parse failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanParse() => !string.IsNullOrWhiteSpace(CronExpression);

    [RelayCommand] private void LoadDaily() { CronExpression = "0 0 * * *"; ParseCommand.Execute(null); }
    [RelayCommand] private void LoadHourly() { CronExpression = "0 * * * *"; ParseCommand.Execute(null); }
    [RelayCommand] private void LoadWeekly() { CronExpression = "0 9 * * 1"; ParseCommand.Execute(null); }
    [RelayCommand] private void LoadMonthly() { CronExpression = "0 0 1 * *"; ParseCommand.Execute(null); }

    [RelayCommand]
    private void CopyResult()
    {
        if (string.IsNullOrWhiteSpace(Result)) { SetStatus(NotificationType.Warning, "L.CronParser.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(Result); SetStatus(NotificationType.Success, "L.CronParser.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveResult()
    {
        if (string.IsNullOrWhiteSpace(Result)) { SetStatus(NotificationType.Warning, "L.CronParser.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.CronParser.Filter.Text"), ".txt", "cron");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, Result); SetStatus(NotificationType.Success, "L.CronParser.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void Clear() { CronExpression = string.Empty; Result = string.Empty; Fields.Clear(); NextRuns.Clear(); SetStatus(NotificationType.Info, "L.CronParser.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CronParser {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record CronFieldRow(string Name, string Raw, string Description);