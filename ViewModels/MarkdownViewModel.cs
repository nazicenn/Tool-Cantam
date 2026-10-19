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

public partial class MarkdownViewModel : ObservableObject
{
    private readonly IMarkdownService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<MarkdownViewModel> _logger;

    [ObservableProperty] private string _inputMarkdown = string.Empty;
    [ObservableProperty] private string _outputHtml = string.Empty;
    [ObservableProperty] private bool _wrapInHtml = true;
    [ObservableProperty] private bool _useAdvancedExtensions = true;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public MarkdownViewModel(IMarkdownService service, IFileExportService fe, ILocalizerService loc, ILogger<MarkdownViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.Markdown.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.Markdown.Limits", _service.MaxInputLength / (1024 * 1024));

    partial void OnInputMarkdownChanged(string value) => ConvertCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ToHtmlAsync(InputMarkdown, new MarkdownOptions(WrapInHtml, UseAdvancedExtensions), ct);
            if (!r.IsSuccess) { ReportFailure(r, "convert"); OutputHtml = string.Empty; return; }
            OutputHtml = r.Value!.Html;
            _logger.LogInformation("Markdown: {In}->{Out}, {Words} words", r.Value.InputLength, r.Value.OutputLength, r.Value.WordCount);
            SetStatus(NotificationType.Success, "L.Markdown.Status.Converted", r.Value.WordCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Markdown failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanConvert() => !string.IsNullOrWhiteSpace(InputMarkdown);

    [RelayCommand]
    private void CopyHtml()
    {
        if (string.IsNullOrWhiteSpace(OutputHtml)) { SetStatus(NotificationType.Warning, "L.Markdown.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(OutputHtml); SetStatus(NotificationType.Success, "L.Markdown.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveHtml()
    {
        if (string.IsNullOrWhiteSpace(OutputHtml)) { SetStatus(NotificationType.Warning, "L.Markdown.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.Markdown.Filter.Html"), ".html", "output");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, OutputHtml); SetStatus(NotificationType.Success, "L.Markdown.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand]
    private void LoadSample()
    {
        InputMarkdown = "# Merhaba\n\nBu **kalın** ve *italik*.\n\n- Madde 1\n- Madde 2\n\n```csharp\nConsole.WriteLine(\"Hello\");\n```\n\n> Alıntı";
        ConvertCommand.Execute(null);
    }

    [RelayCommand]
    private void Clear() { InputMarkdown = string.Empty; OutputHtml = string.Empty; SetStatus(NotificationType.Info, "L.Markdown.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("Markdown {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}