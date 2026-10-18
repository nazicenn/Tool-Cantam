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

public partial class CsvToJsonViewModel : ObservableObject
{
    private readonly ICsvToJsonService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CsvToJsonViewModel> _logger;

    [ObservableProperty] private string _inputCsv = string.Empty;
    [ObservableProperty] private string _outputJson = string.Empty;
    [ObservableProperty] private char _delimiter = ',';
    [ObservableProperty] private bool _firstRowAsHeader = true;
    [ObservableProperty] private bool _trimValues = true;
    [ObservableProperty] private bool _detectTypes = true;
    [ObservableProperty] private int _indentSize = 2;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public CsvToJsonViewModel(ICsvToJsonService service, IFileExportService fe, ILocalizerService loc, ILogger<CsvToJsonViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CsvToJson.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.CsvToJson.Limits", _service.MaxInputLength / (1024 * 1024));
    public char[] Delimiters => new[] { ',', ';', '\t', '|' };
    public int[] IndentSizes => new[] { 0, 2, 4 };

    partial void OnInputCsvChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new CsvOptions(Delimiter, FirstRowAsHeader, TrimValues, DetectTypes, IndentSize);
            var r = await _service.ConvertAsync(InputCsv, opts, ct);
            if (!r.IsSuccess) { ReportFailure(r, "convert"); OutputJson = string.Empty; return; }
            OutputJson = r.Value!.OutputJson;
            _logger.LogInformation("CSV→JSON: rows={R} cols={C} in {Ms:0}ms", r.Value.Rows, r.Value.Columns, r.Value.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.CsvToJson.Status.Converted", r.Value.Rows, r.Value.Columns);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "CSV convert failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanConvert() => !string.IsNullOrWhiteSpace(InputCsv);

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.CsvToJson.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputJson); SetStatus(NotificationType.Success, "L.CsvToJson.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.CsvToJson.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.CsvToJson.Filter.Json"), ".json", "converted");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputJson); SetStatus(NotificationType.Success, "L.CsvToJson.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void LoadSample() { InputCsv = "name,age,city\nAhmet,30,İstanbul\nMehmet,25,Ankara\nAyşe,28,İzmir"; ConvertCommand.Execute(null); }

    [RelayCommand]
    private void Clear() { InputCsv = string.Empty; OutputJson = string.Empty; SetStatus(NotificationType.Info, "L.CsvToJson.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CsvToJson {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}