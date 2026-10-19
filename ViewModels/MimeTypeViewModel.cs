using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetadataExtractor.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class MimeTypeViewModel : ObservableObject
{
    private readonly IMimeTypeService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<MimeTypeViewModel> _logger;

    [ObservableProperty] private string _input = string.Empty;
    [ObservableProperty] private string _mimeType = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private string _flags = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> AllExtensions { get; } = new();
    public ObservableCollection<string> AllMimeTypes { get; } = new();

    public MimeTypeViewModel(IMimeTypeService service, IFileExportService fe, ILocalizerService loc, ILogger<MimeTypeViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.MimeType.Status.Ready");
        foreach (var e in _service.AllExtensions) AllExtensions.Add(e);
        foreach (var m in _service.AllMimeTypes) AllMimeTypes.Add(m);
    }

    public string LimitsText => _localizer.Format("L.MimeType.Limits", _service.MaxInputLength);

    partial void OnInputChanged(string value) => LookupCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanLookup))]
    private async Task LookupAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            // Uzantı mı MIME mi? "/" varsa MIME, yoksa uzantı
            var isMime = Input.Contains('/');
            var r = isMime
                ? await _service.LookupByMimeAsync(Input, ct)
                : await _service.LookupByExtensionAsync(Input, ct);

            if (!r.IsSuccess) { ReportFailure(r, "lookup"); ClearResults(); return; }
            var v = r.Value!;
            MimeType = v.MimeType;
            Description = v.Description;
            Category = v.Category;
            var flags = new List<string>();
            if (v.IsText) flags.Add("text");
            if (v.IsImage) flags.Add("image");
            if (v.IsAudio) flags.Add("audio");
            if (v.IsVideo) flags.Add("video");
            if (v.IsArchive) flags.Add("archive");
            if (v.IsCompressed) flags.Add("compressed");
            Flags = string.Join(", ", flags);
            _logger.LogInformation("MIME lookup: {Input} → {Mime}", Input, v.MimeType);
            SetStatus(NotificationType.Success, "L.MimeType.Status.Found", v.MimeType);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "MIME lookup failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanLookup() => !string.IsNullOrWhiteSpace(Input);

    [RelayCommand] private void CopyMime() => Copy(MimeType, "L.MimeType.Status.CopiedMime");
    [RelayCommand] private void CopyInput() => Copy(Input, "L.MimeType.Status.CopiedInput");

    private void Copy(string text, string successKey)
    {
        if (string.IsNullOrWhiteSpace(text)) { SetStatus(NotificationType.Warning, "L.MimeType.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(text); SetStatus(NotificationType.Success, successKey); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear() { Input = string.Empty; ClearResults(); SetStatus(NotificationType.Info, "L.MimeType.Status.Cleared"); }

    private void ClearResults() { MimeType = Description = Category = Flags = string.Empty; }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("MimeType {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}