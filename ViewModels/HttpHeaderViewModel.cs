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

/// <summary>HTTP Header Analyzer sunum katmanı.</summary>
public partial class HttpHeaderViewModel : ObservableObject
{
    private readonly IHttpHeaderService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HttpHeaderViewModel> _logger;

    [ObservableProperty] private string _targetUrl = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _summaryText = string.Empty;

    public ObservableCollection<HeaderRow> Headers { get; } = new();

    public HttpHeaderViewModel(
        IHttpHeaderService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HttpHeaderViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.HttpHeader.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.HttpHeader.Limits",
        _service.RequestTimeout.TotalSeconds, _service.MaxHeaders);

    partial void OnTargetUrlChanged(string value) => AnalyzeCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanExecuteAnalyze))]
    private async Task AnalyzeAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.AnalyzeAsync(TargetUrl, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "analyze");
                Headers.Clear();
                SummaryText = string.Empty;
                return;
            }

            var report = result.Value!;
            Headers.Clear();
            foreach (var h in report.Headers)
                Headers.Add(new HeaderRow(h.Name, h.Value, h.Category.ToString()));

            SummaryText = _localizer.Format("L.HttpHeader.Summary",
                report.StatusCode, report.ReasonPhrase, report.Server, report.Elapsed.TotalMilliseconds);

            _logger.LogInformation("HTTP headers: status={Status} count={Count} elapsed={Ms}ms getFallback={Fallback}",
                report.StatusCode, report.Headers.Count, report.Elapsed.TotalMilliseconds, report.UsedGetFallback);

            SetStatus(NotificationType.Success, "L.HttpHeader.Status.Analyzed", report.Headers.Count);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP header analyze failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanExecuteAnalyze() => !string.IsNullOrWhiteSpace(TargetUrl);

    [RelayCommand]
    private void CopyResults()
    {
        if (Headers.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.HttpHeader.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(BuildText());
            SetStatus(NotificationType.Success, "L.HttpHeader.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clipboard failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveResults()
    {
        if (Headers.Count == 0)
        {
            SetStatus(NotificationType.Warning, "L.HttpHeader.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.HttpHeader.Filter.Text"),
            ".txt",
            $"headers_{SanitizeFileName(TargetUrl)}");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, BuildText(), Encoding.UTF8);
            SetStatus(NotificationType.Success, "L.HttpHeader.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TargetUrl = string.Empty;
        Headers.Clear();
        SummaryText = string.Empty;
        SetStatus(NotificationType.Info, "L.HttpHeader.Status.Cleared");
    }

    private string BuildText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"URL: {TargetUrl}");
        if (!string.IsNullOrWhiteSpace(SummaryText)) sb.AppendLine(SummaryText);
        sb.AppendLine(new string('-', 60));
        foreach (var h in Headers)
            sb.AppendLine($"{h.Name}: {h.Value}");
        return sb.ToString();
    }

    private static string SanitizeFileName(string url)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            url = url.Replace(c, '_');
        return url.Length > 40 ? url[..40] : url;
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("HttpHeader {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}

/// <summary>UI satırı.</summary>
public sealed record HeaderRow(string Name, string Value, string Category);