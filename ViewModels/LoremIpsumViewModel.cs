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

public partial class LoremIpsumViewModel : ObservableObject
{
    private readonly ILoremIpsumService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<LoremIpsumViewModel> _logger;

    [ObservableProperty] private int _paragraphCount = 3;
    [ObservableProperty] private int _sentenceCount = 5;
    [ObservableProperty] private bool _startWithLorem = true;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public LoremIpsumViewModel(ILoremIpsumService service, IFileExportService fe, ILocalizerService loc, ILogger<LoremIpsumViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.LoremIpsum.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.LoremIpsum.Limits",
        _service.MinParagraphs, _service.MaxParagraphs, _service.MinSentences, _service.MaxSentences);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.GenerateAsync(new LoremOptions(ParagraphCount, SentenceCount, StartWithLorem), ct);
            if (!r.IsSuccess) { ReportFailure(r, "gen"); OutputText = string.Empty; return; }
            OutputText = r.Value!.Text;
            _logger.LogInformation("Lorem generated: {P} paragraphs, {W} words", r.Value.ParagraphCount, r.Value.WordCount);
            SetStatus(NotificationType.Success, "L.LoremIpsum.Status.Generated", r.Value.ParagraphCount, r.Value.WordCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Lorem gen failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText)) { SetStatus(NotificationType.Warning, "L.LoremIpsum.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputText); SetStatus(NotificationType.Success, "L.LoremIpsum.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText)) { SetStatus(NotificationType.Warning, "L.LoremIpsum.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.LoremIpsum.Filter.Text"), ".txt", "lorem");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputText); SetStatus(NotificationType.Success, "L.LoremIpsum.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void Clear() { OutputText = string.Empty; SetStatus(NotificationType.Info, "L.LoremIpsum.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("LoremIpsum {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}