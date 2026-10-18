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

public partial class CurlGeneratorViewModel : ObservableObject
{
    private readonly ICurlGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CurlGeneratorViewModel> _logger;

    [ObservableProperty] private string _url = "https://jsonplaceholder.typicode.com/posts/1";
    [ObservableProperty] private string _method = "GET";
    [ObservableProperty] private string _headers = "Content-Type: application/json";
    [ObservableProperty] private string _body = string.Empty;
    [ObservableProperty] private bool _includeHeaders = true;
    [ObservableProperty] private bool _includeBody = true;
    [ObservableProperty] private bool _useLongFlags;
    [ObservableProperty] private bool _insecure;
    [ObservableProperty] private bool _isBodyVisible;
    [ObservableProperty] private string _curlCommand = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public CurlGeneratorViewModel(ICurlGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<CurlGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CurlGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Get("L.CurlGenerator.Limits");
    public string[] Methods => new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD" };

    partial void OnMethodChanged(string value) => IsBodyVisible = value is "POST" or "PUT" or "PATCH";

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new CurlOptions(Url, Method, Headers, Body, IncludeHeaders, IncludeBody, UseLongFlags, Insecure);
            var r = await _service.GenerateAsync(opts, ct);
            if (!r.IsSuccess) { ReportFailure(r, "gen"); CurlCommand = string.Empty; return; }
            CurlCommand = r.Value!.Command;
            _logger.LogInformation("cURL generated: {Lines} lines in {Ms:0}ms", r.Value.LineCount, r.Value.Elapsed.TotalMilliseconds);
            SetStatus(NotificationType.Success, "L.CurlGenerator.Status.Generated", r.Value.LineCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "cURL gen failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanGenerate() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand]
    private void CopyCurl()
    {
        if (string.IsNullOrWhiteSpace(CurlCommand)) { SetStatus(NotificationType.Warning, "L.CurlGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(CurlCommand); SetStatus(NotificationType.Success, "L.CurlGenerator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveCurl()
    {
        if (string.IsNullOrWhiteSpace(CurlCommand)) { SetStatus(NotificationType.Warning, "L.CurlGenerator.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.CurlGenerator.Filter.Script"), ".sh", "request");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, CurlCommand); SetStatus(NotificationType.Success, "L.CurlGenerator.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void LoadGet() { Url = "https://jsonplaceholder.typicode.com/posts/1"; Method = "GET"; Body = ""; Headers = "Content-Type: application/json"; GenerateCommand.Execute(null); }
    [RelayCommand] private void LoadPost() { Url = "https://jsonplaceholder.typicode.com/posts"; Method = "POST"; Headers = "Content-Type: application/json"; Body = "{\n  \"title\": \"Test\",\n  \"body\": \"Content\",\n  \"userId\": 1\n}"; GenerateCommand.Execute(null); }
    [RelayCommand] private void Clear() { Url = string.Empty; Headers = string.Empty; Body = string.Empty; CurlCommand = string.Empty; SetStatus(NotificationType.Info, "L.CurlGenerator.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CurlGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}