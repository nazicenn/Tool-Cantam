using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class UserAgentParserViewModel : ObservableObject
{
    private readonly IUserAgentParserService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<UserAgentParserViewModel> _logger;

    [ObservableProperty] private string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
    [ObservableProperty] private string _browser = string.Empty;
    [ObservableProperty] private string _browserVersion = string.Empty;
    [ObservableProperty] private string _operatingSystem = string.Empty;
    [ObservableProperty] private string _osVersion = string.Empty;
    [ObservableProperty] private string _deviceType = string.Empty;
    [ObservableProperty] private string _deviceVendor = string.Empty;
    [ObservableProperty] private bool _isMobile;
    [ObservableProperty] private bool _isBot;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public UserAgentParserViewModel(IUserAgentParserService service, IFileExportService fe, ILocalizerService loc, ILogger<UserAgentParserViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.UserAgentParser.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.UserAgentParser.Limits", _service.MaxInputLength);

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanParse))]
    private async Task ParseAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.ParseAsync(UserAgent, ct);
            if (!r.IsSuccess) { ReportFailure(r, "parse"); ClearResults(); return; }
            var v = r.Value!;
            Browser = v.Browser;
            BrowserVersion = v.BrowserVersion;
            OperatingSystem = v.OperatingSystem;
            OsVersion = v.OsVersion;
            DeviceType = v.DeviceType;
            DeviceVendor = v.DeviceVendor;
            IsMobile = v.IsMobile;
            IsBot = v.IsBot;
            _logger.LogInformation("UA parsed: {Browser} {Ver}, {Os}", v.Browser, v.BrowserVersion, v.OperatingSystem);
            SetStatus(NotificationType.Success, "L.UserAgentParser.Status.Parsed", v.Browser, v.OperatingSystem);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "UA parse failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanParse() => !string.IsNullOrWhiteSpace(UserAgent);

    [RelayCommand]
    private void CopyUserAgent()
    {
        if (string.IsNullOrWhiteSpace(UserAgent)) { SetStatus(NotificationType.Warning, "L.UserAgentParser.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(UserAgent); SetStatus(NotificationType.Success, "L.UserAgentParser.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        UserAgent = string.Empty;
        ClearResults();
        SetStatus(NotificationType.Info, "L.UserAgentParser.Status.Cleared");
    }

    private void ClearResults()
    {
        Browser = BrowserVersion = OperatingSystem = OsVersion = DeviceType = DeviceVendor = string.Empty;
        IsMobile = IsBot = false;
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("UserAgent {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}