using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;
using ZXing.QrCode.Internal;

namespace tool_cantam.ViewModels;

/// <summary>
/// Unix Timestamp aracının sunum katmanı. Tüm iş mantığı <see cref="IUnixTimestampService"/> içindedir.
/// </summary>
public partial class UnixTimestampViewModel : ObservableObject
{
    private readonly IUnixTimestampService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<UnixTimestampViewModel> _logger;

    [ObservableProperty] private string _timestampInput = string.Empty;
    [ObservableProperty] private string _dateInput = string.Empty;

    [ObservableProperty] private string _readableOutput = string.Empty;
    [ObservableProperty] private string _isoOutput = string.Empty;
    [ObservableProperty] private string _rfcOutput = string.Empty;
    [ObservableProperty] private string _unixSecondsOutput = string.Empty;
    [ObservableProperty] private string _unixMillisOutput = string.Empty;

    [ObservableProperty] private TimestampUnit _unit = TimestampUnit.Seconds;
    [ObservableProperty] private TimeDisplayMode _mode = TimeDisplayMode.Utc;

    [ObservableProperty] private string _currentTimestamp = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public UnixTimestampViewModel(
        IUnixTimestampService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<UnixTimestampViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        UpdateCurrent();
        StatusText = _localizer.Get("L.UnixTimestamp.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.UnixTimestamp.Limits",
        _service.MinSeconds, _service.MaxSeconds);

    public TimestampUnit[] Units => new[] { TimestampUnit.Seconds, TimestampUnit.Milliseconds };
    public TimeDisplayMode[] Modes => new[] { TimeDisplayMode.Utc, TimeDisplayMode.Local };

    private void UpdateCurrent()
    {
        var now = _service.Now();
        CurrentTimestamp = now.UnixSeconds.ToString();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TimestampToDateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.FromUnixAsync(TimestampInput, Unit, Mode, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "fromUnix");
                ClearDateOutputs();
                return;
            }

            var r = result.Value!;
            ReadableOutput = r.Readable;
            IsoOutput = r.Iso8601;
            RfcOutput = r.Rfc1123;
            UnixSecondsOutput = r.UnixSeconds.ToString();
            UnixMillisOutput = r.UnixMilliseconds.ToString();

            _logger.LogInformation("Unix→Date ok: seconds={S} mode={Mode}", r.UnixSeconds, r.Mode);
            SetStatus(NotificationType.Success, "L.UnixTimestamp.Status.ConvertedToDate", r.Readable);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unix→Date failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DateToTimestampAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var result = await _service.ToUnixAsync(DateInput, Mode, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "toUnix");
                ClearDateOutputs();
                return;
            }

            var r = result.Value!;
            ReadableOutput = r.Readable;
            IsoOutput = r.Iso8601;
            RfcOutput = r.Rfc1123;
            UnixSecondsOutput = r.UnixSeconds.ToString();
            UnixMillisOutput = r.UnixMilliseconds.ToString();

            _logger.LogInformation("Date→Unix ok: seconds={S} mode={Mode}", r.UnixSeconds, r.Mode);
            SetStatus(NotificationType.Success, "L.UnixTimestamp.Status.ConvertedToTimestamp", r.UnixSeconds);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Date→Unix failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void UseNow()
    {
        UpdateCurrent();
        TimestampInput = CurrentTimestamp;
        DateInput = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        SetStatus(NotificationType.Info, "L.UnixTimestamp.Status.NowLoaded");
    }

    [RelayCommand]
    private void CopyReadable() => CopyToClipboard(ReadableOutput, "L.UnixTimestamp.Status.CopiedReadable");
    [RelayCommand]
    private void CopyIso() => CopyToClipboard(IsoOutput, "L.UnixTimestamp.Status.CopiedIso");
    [RelayCommand]
    private void CopyRfc() => CopyToClipboard(RfcOutput, "L.UnixTimestamp.Status.CopiedRfc");
    [RelayCommand]
    private void CopySeconds() => CopyToClipboard(UnixSecondsOutput, "L.UnixTimestamp.Status.CopiedSeconds");
    [RelayCommand]
    private void CopyMillis() => CopyToClipboard(UnixMillisOutput, "L.UnixTimestamp.Status.CopiedMillis");

    private void CopyToClipboard(string text, string successKey)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus(NotificationType.Warning, "L.UnixTimestamp.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(text);
            SetStatus(NotificationType.Success, successKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clipboard copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TimestampInput = string.Empty;
        DateInput = string.Empty;
        ClearDateOutputs();
        UpdateCurrent();
        SetStatus(NotificationType.Info, "L.UnixTimestamp.Status.Cleared");
    }

    private void ClearDateOutputs()
    {
        ReadableOutput = string.Empty;
        IsoOutput = string.Empty;
        RfcOutput = string.Empty;
        UnixSecondsOutput = string.Empty;
        UnixMillisOutput = string.Empty;
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("UnixTimestamp {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}