using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class SqlFormatterViewModel : ObservableObject
{
    private readonly ISqlFormatterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SqlFormatterViewModel> _logger;

    [ObservableProperty] private string _inputSql = string.Empty;
    [ObservableProperty] private string _outputSql = string.Empty;
    [ObservableProperty] private bool _uppercaseKeywords = true;
    [ObservableProperty] private int _indentSize = 2;
    [ObservableProperty] private bool _newlineBeforeAndOr;
    [ObservableProperty] private bool _commaNewline;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public SqlFormatterViewModel(
        ISqlFormatterService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<SqlFormatterViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.SqlFormatter.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.SqlFormatter.Limits", _service.MaxInputLength / (1024 * 1024));
    public int[] IndentSizes => new[] { 2, 4, 8 };

    partial void OnInputSqlChanged(string value) => FormatCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanFormat))]
    private async Task FormatAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new SqlOptions(UppercaseKeywords, IndentSize, NewlineBeforeAndOr, CommaNewline);
            var result = await _service.FormatAsync(InputSql, opts, ct);
            if (!result.IsSuccess) { ReportFailure(result, "format"); OutputSql = string.Empty; return; }
            OutputSql = result.Value!.Output;
            _logger.LogInformation("SQL formatted: {In}->{Out} in {Ms:0}ms", result.Value.OriginalLength, result.Value.OutputLength, result.Value.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.SqlFormatter.Status.Formatted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "SQL format failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanFormat() => !string.IsNullOrWhiteSpace(InputSql);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanFormat))]
    private async Task MinifyAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var result = await _service.MinifyAsync(InputSql, ct);
            if (!result.IsSuccess) { ReportFailure(result, "minify"); OutputSql = string.Empty; return; }
            OutputSql = result.Value!.Output;
            _logger.LogInformation("SQL minified: {In}->{Out}", result.Value.OriginalLength, result.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.SqlFormatter.Status.Minified");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "SQL minify failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var result = await _service.ValidateAsync(InputSql, CancellationToken.None);
        if (!result.IsSuccess) { ReportFailure(result, "validate"); return; }
        var v = result.Value!;
        if (v.IsValid)
            SetStatus(NotificationType.Success, "L.SqlFormatter.Status.Valid", v.StatementCount);
        else
            SetStatus(NotificationType.Error, "L.SqlFormatter.Status.Invalid", v.Error ?? "?");
    }

    [RelayCommand]
    private void Swap() { (InputSql, OutputSql) = (OutputSql, InputSql); SetStatus(NotificationType.Info, "L.SqlFormatter.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputSql)) { SetStatus(NotificationType.Warning, "L.SqlFormatter.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputSql); SetStatus(NotificationType.Success, "L.SqlFormatter.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputSql)) { SetStatus(NotificationType.Warning, "L.SqlFormatter.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.SqlFormatter.Filter.Sql"), ".sql", "formatted");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputSql); SetStatus(NotificationType.Success, "L.SqlFormatter.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void LoadSample() { InputSql = "SELECT id,name,email,city FROM users WHERE age>18 AND status='active' ORDER BY name"; FormatCommand.Execute(null); }

    [RelayCommand]
    private void Clear() { InputSql = string.Empty; OutputSql = string.Empty; SetStatus(NotificationType.Info, "L.SqlFormatter.Status.Cleared"); }

    private void SetStatus(NotificationType type, string key, params object?[] args) { StatusType = type; StatusText = _localizer.Format(key, args); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("SqlFormatter {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}