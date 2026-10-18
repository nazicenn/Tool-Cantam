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

public partial class XmlFormatterViewModel : ObservableObject
{
    private readonly IXmlFormatterService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<XmlFormatterViewModel> _logger;

    [ObservableProperty] private string _inputXml = string.Empty;
    [ObservableProperty] private string _outputXml = string.Empty;
    [ObservableProperty] private int _indentSize = 2;
    [ObservableProperty] private bool _omitDeclaration;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public XmlFormatterViewModel(IXmlFormatterService service, IFileExportService fe, ILocalizerService loc, ILogger<XmlFormatterViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.XmlFormatter.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.XmlFormatter.Limits", _service.MaxInputLength / (1024 * 1024));
    public int[] IndentSizes => new[] { 2, 4, 8 };

    partial void OnInputXmlChanged(string value) => FormatCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanFormat))]
    private async Task FormatAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.FormatAsync(InputXml, new XmlOptions(IndentSize, OmitDeclaration), ct);
            if (!r.IsSuccess) { ReportFailure(r, "format"); OutputXml = string.Empty; return; }
            OutputXml = r.Value!.Output;
            _logger.LogInformation("XML formatted: {In}->{Out}", r.Value.OriginalLength, r.Value.OutputLength);
            SetStatus(NotificationType.Success, "L.XmlFormatter.Status.Formatted");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "XML format failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanFormat() => !string.IsNullOrWhiteSpace(InputXml);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanFormat))]
    private async Task MinifyAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.MinifyAsync(InputXml, ct);
            if (!r.IsSuccess) { ReportFailure(r, "minify"); OutputXml = string.Empty; return; }
            OutputXml = r.Value!.Output;
            SetStatus(NotificationType.Success, "L.XmlFormatter.Status.Minified");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "XML minify failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var r = await _service.ValidateAsync(InputXml, CancellationToken.None);
        if (!r.IsSuccess) { ReportFailure(r, "validate"); return; }
        var v = r.Value!;
        if (v.IsValid)
            SetStatus(NotificationType.Success, "L.XmlFormatter.Status.Valid", v.ElementCount, v.AttributeCount);
        else
            SetStatus(NotificationType.Error, "L.XmlFormatter.Status.Invalid", v.Error ?? "?");
    }

    [RelayCommand] private void Swap() { (InputXml, OutputXml) = (OutputXml, InputXml); SetStatus(NotificationType.Info, "L.XmlFormatter.Status.Swapped"); }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputXml)) { SetStatus(NotificationType.Warning, "L.XmlFormatter.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputXml); SetStatus(NotificationType.Success, "L.XmlFormatter.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputXml)) { SetStatus(NotificationType.Warning, "L.XmlFormatter.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.XmlFormatter.Filter.Xml"), ".xml", "formatted");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputXml); SetStatus(NotificationType.Success, "L.XmlFormatter.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadSample() { InputXml = "<?xml version=\"1.0\"?><root><user id=\"1\"><name>Ahmet</name><city>İstanbul</city></user><user id=\"2\"><name>Mehmet</name><city>Ankara</city></user></root>"; FormatCommand.Execute(null); }
    [RelayCommand] private void Clear() { InputXml = string.Empty; OutputXml = string.Empty; SetStatus(NotificationType.Info, "L.XmlFormatter.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("XmlFormatter {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}