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

public partial class YamlToJsonViewModel : ObservableObject
{
    private readonly IYamlToJsonService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<YamlToJsonViewModel> _logger;

    [ObservableProperty] private string _inputYaml = string.Empty;
    [ObservableProperty] private string _outputJson = string.Empty;
    [ObservableProperty] private bool _indented = true;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public YamlToJsonViewModel(IYamlToJsonService service, IFileExportService fe, ILocalizerService loc, ILogger<YamlToJsonViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.YamlToJson.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.YamlToJson.Limits", _service.MaxInputLength / (1024 * 1024));

    partial void OnInputYamlChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ConvertAsync(InputYaml, new YamlToJsonOptions(Indented), ct);
            if (!r.IsSuccess) { ReportFailure(r, "convert"); OutputJson = string.Empty; return; }
            OutputJson = r.Value!.Json;
            _logger.LogInformation("YAML→JSON: {In}->{Out}", r.Value.InputLength, r.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.YamlToJson.Status.Converted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "YAML→JSON failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanConvert() => !string.IsNullOrWhiteSpace(InputYaml);

    [RelayCommand] private void Swap() { (InputYaml, OutputJson) = (OutputJson, InputYaml); SetStatus(NotificationType.Info, "L.YamlToJson.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.YamlToJson.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputJson); SetStatus(NotificationType.Success, "L.YamlToJson.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputJson)) { SetStatus(NotificationType.Warning, "L.YamlToJson.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.YamlToJson.Filter.Json"), ".json", "output");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputJson); SetStatus(NotificationType.Success, "L.YamlToJson.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadSample() { InputYaml = "name: John Doe\nage: 30\nemail: john@example.com\naddress:\n  city: Istanbul\n  country: Turkey\nhobbies:\n  - coding\n  - gaming"; ConvertCommand.Execute(null); }
    [RelayCommand] private void Clear() { InputYaml = string.Empty; OutputJson = string.Empty; SetStatus(NotificationType.Info, "L.YamlToJson.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("YamlToJson {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}