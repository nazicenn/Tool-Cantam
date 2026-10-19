using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class RestApiClientViewModel : ObservableObject
{
    private readonly IRestApiService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<RestApiClientViewModel> _logger;

    [ObservableProperty] private string _url = "https://jsonplaceholder.typicode.com/posts/1";
    [ObservableProperty] private string _method = "GET";
    [ObservableProperty] private string _headers = "Content-Type: application/json";
    [ObservableProperty] private string _body = string.Empty;
    [ObservableProperty] private string _responseBody = string.Empty;
    [ObservableProperty] private int _statusCode;
    [ObservableProperty] private string _statusDescription = string.Empty;
    [ObservableProperty] private long _responseTimeMs;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isBodyVisible;

    public ObservableCollection<RestHeaderRow> ResponseHeaders { get; } = new();

    public RestApiClientViewModel(IRestApiService service, IFileExportService fe, ILocalizerService loc, ILogger<RestApiClientViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.RestApi.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.RestApi.Limits", _service.RequestTimeout.TotalSeconds);
    public string[] Methods => new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD" };

    partial void OnMethodChanged(string value) => IsBodyVisible = value is "POST" or "PUT" or "PATCH";

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.SendAsync(new RestApiOptions(Url, Method, Headers, Body), ct);
            if (!r.IsSuccess) { ReportFailure(r, "send"); ClearResponse(); return; }
            var v = r.Value!;
            StatusCode = v.StatusCode;
            StatusDescription = v.ReasonPhrase;
            ResponseBody = PrettyJson(v.Body);
            ResponseTimeMs = (long)v.Elapsed.TotalMilliseconds;
            ResponseHeaders.Clear();
            foreach (var h in v.Headers) ResponseHeaders.Add(new RestHeaderRow(h.Key, h.Value));
            _logger.LogInformation("REST {Method} {Url} → {Status} in {Ms}ms", Method, Url, v.StatusCode, ResponseTimeMs);
            SetStatus(v.StatusCode < 400 ? NotificationType.Success : NotificationType.Warning,
                "L.RestApi.Status.Completed", v.StatusCode, ResponseTimeMs);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "REST failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand] private void LoadGet() { Url = "https://jsonplaceholder.typicode.com/posts/1"; Method = "GET"; Body = ""; SetStatus(NotificationType.Info, "L.RestApi.Status.SampleLoaded"); }
    [RelayCommand] private void LoadPost() { Url = "https://jsonplaceholder.typicode.com/posts"; Method = "POST"; Headers = "Content-Type: application/json"; Body = "{\n  \"title\": \"Test\",\n  \"body\": \"Content\",\n  \"userId\": 1\n}"; SetStatus(NotificationType.Info, "L.RestApi.Status.SampleLoaded"); }

    [RelayCommand]
    private void CopyResponse()
    {
        if (string.IsNullOrWhiteSpace(ResponseBody)) { SetStatus(NotificationType.Warning, "L.RestApi.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(ResponseBody); SetStatus(NotificationType.Success, "L.RestApi.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void SaveResponse()
    {
        if (string.IsNullOrWhiteSpace(ResponseBody)) { SetStatus(NotificationType.Warning, "L.RestApi.Validation.NothingToSave"); return; }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.RestApi.Filter.Json"), ".json", "response");
        if (string.IsNullOrEmpty(path)) return;
        try { File.WriteAllText(path, ResponseBody); SetStatus(NotificationType.Success, "L.RestApi.Status.Saved", Path.GetFileName(path)); }
        catch (Exception ex) { _logger.LogError(ex, "Save failed."); SetStatus(NotificationType.Error, "L.Common.Error.FileSave"); }
    }

    [RelayCommand] private void Clear() { ClearResponse(); SetStatus(NotificationType.Info, "L.RestApi.Status.Cleared"); }

    private void ClearResponse() { ResponseBody = string.Empty; StatusCode = 0; StatusDescription = string.Empty; ResponseTimeMs = 0; ResponseHeaders.Clear(); }

    private static string PrettyJson(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(s);
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch { return s; }
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("RestApi {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}

public sealed record RestHeaderRow(string Name, string Value);