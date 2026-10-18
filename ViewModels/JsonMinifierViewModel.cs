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

public partial class JsonMinifierViewModel : ObservableObject
{
    private readonly IJsonMinifierService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<JsonMinifierViewModel> _logger;

    [ObservableProperty] private string _inputJson = string.Empty;
    [ObservableProperty] private string _outputJson = string.Empty;
    [ObservableProperty] private int _indentSize = 2;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public JsonMinifierViewModel(IJsonMinifierService service, IFileExportService fe, ILocalizerService loc, ILogger<JsonMinifierViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.JsonMinifier.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.JsonMinifier.Limits", _service.MaxInputLength / (1024 * 1024));
    public int[] IndentSizes => new[] { 2, 4, 8 };

    partial void OnInputJsonChanged(string value) => MinifyCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanProcess))]
    private async Task MinifyAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.MinifyAsync(InputJson, ct);
            if (!r.IsSuccess) { ReportFailure(r, "minify"); OutputJson = string.Empty; return; }
            OutputJson = r.Value!.Output;
            _logger.LogInformation("JSON minified: {In}->{Out}, nodes={Nodes}", r.Value.OriginalLength, r.Value.OutputLength, r.Value.NodeCount);
            SetStatus(NotificationType.Success, "L.JsonMinifier.Status.Minified", r.Value.NodeCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "JSON minify failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanProcess() => !string.IsNullOrWhiteSpace(InputJson);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanProcess))]
    private async Task FormatAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.FormatAsync(InputJson, IndentSize, ct);
            if (!r.IsSuccess) { ReportFailure(r, "format"); OutputJson = string.Empty; return; }
            OutputJson = r.Value!.Output;
            _logger.LogInformation("JSON formatted: {In}->{Out}, nodes={Nodes}", r.Value.OriginalLength, r.Value.OutputLength, r.Value.NodeCount);
            SetStatus(NotificationType.Success, "L.JsonMinifier.Status.Formatted", r.Value.NodeCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "JSON format failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void Swap() { (InputJson, OutputJson) = (OutputJson, InputJson); SetStatus(NotificationType.Info, "L.JsonMinifier.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.JsonMinifier.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputJson); SetStatus(NotificationType.Success, "L.JsonMinifier.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.JsonMinifier.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.JsonMinifier.Filter.Json"), ".json", "output");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputJson); SetStatus(NotificationType.Success, "L.JsonMinifier.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadSample() { InputJson = "{\n  \"name\": \"John\",\n  \"age\": 30,\n  \"tags\": [\"a\", \"b\"]\n}"; MinifyCommand.Execute(null); }
    [RelayCommand] private void Clear() { InputJson = string.Empty; OutputJson = string.Empty; SetStatus(NotificationType.Info, "L.JsonMinifier.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("JsonMinifier {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}