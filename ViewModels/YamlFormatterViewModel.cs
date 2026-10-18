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

public partial class YamlFormatterViewModel : ObservableObject
{
    private readonly IYamlFormatterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<YamlFormatterViewModel> _logger;

    [ObservableProperty] private string _inputYaml = string.Empty;
    [ObservableProperty] private string _outputYaml = string.Empty;
    [ObservableProperty] private int _indentSize = 2;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public YamlFormatterViewModel(IYamlFormatterService service, IFileExportService fe, ILocalizerService loc, ILogger<YamlFormatterViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.YamlFormatter.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.YamlFormatter.Limits", _service.MaxInputLength / (1024 * 1024));
    public int[] IndentSizes => new[] { 2, 4, 8 };

    partial void OnInputYamlChanged(string value) => FormatCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanProcess))]
    private async Task FormatAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.FormatAsync(InputYaml, IndentSize, ct);
            if (!r.IsSuccess) { ReportFailure(r, "format"); OutputYaml = string.Empty; return; }
            OutputYaml = r.Value!.Output;
            _logger.LogInformation("YAML formatted: {In}->{Out}", r.Value.OriginalLength, r.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.YamlFormatter.Status.Formatted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "YAML format failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanProcess() => !string.IsNullOrWhiteSpace(InputYaml);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanProcess))]
    private async Task MinifyAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.MinifyAsync(InputYaml, ct);
            if (!r.IsSuccess) { ReportFailure(r, "minify"); OutputYaml = string.Empty; return; }
            OutputYaml = r.Value!.Output;
            SetStatus(NotificationType.Success, "L.YamlFormatter.Status.Minified");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "YAML minify failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void Swap() { (InputYaml, OutputYaml) = (OutputYaml, InputYaml); SetStatus(NotificationType.Info, "L.YamlFormatter.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputYaml)) { SetStatus(NotificationType.Warning, "L.YamlFormatter.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputYaml); SetStatus(NotificationType.Success, "L.YamlFormatter.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputYaml)) { SetStatus(NotificationType.Warning, "L.YamlFormatter.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.YamlFormatter.Filter.Yaml"), ".yaml", "formatted");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputYaml); SetStatus(NotificationType.Success, "L.YamlFormatter.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadSample() { InputYaml = "name: John Doe\nage: 30\nemail: john@example.com\naddress:\n  city: Istanbul\n  country: Turkey\nhobbies:\n  - coding\n  - gaming"; FormatCommand.Execute(null); }
    [RelayCommand] private void Clear() { InputYaml = string.Empty; OutputYaml = string.Empty; SetStatus(NotificationType.Info, "L.YamlFormatter.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("YamlFormatter {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}