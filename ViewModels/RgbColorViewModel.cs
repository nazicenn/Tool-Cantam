using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class RgbColorViewModel : ObservableObject
{
    private readonly IRgbColorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<RgbColorViewModel> _logger;

    [ObservableProperty] private int _red;
    [ObservableProperty] private int _green;
    [ObservableProperty] private int _blue;
    [ObservableProperty] private string _hexValue = "#000000";
    [ObservableProperty] private string _rgbValue = "rgb(0, 0, 0)";
    [ObservableProperty] private string _hslValue = "hsl(0, 0%, 0%)";
    [ObservableProperty] private string _colorInfo = string.Empty;
    [ObservableProperty] private Color _previewColor = Colors.Black;
    [ObservableProperty] private Color _previewTextColor = Colors.White;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    private bool _suppressAutoConvert;

    public RgbColorViewModel(IRgbColorService service, IFileExportService fe, ILocalizerService loc, ILogger<RgbColorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.RgbColor.Status.Ready");
        _ = ConvertFromRgbAsync();
    }

    public string LimitsText => _localizer.Get("L.RgbColor.Limits");

    partial void OnRedChanged(int value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }
    partial void OnGreenChanged(int value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }
    partial void OnBlueChanged(int value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }

    private async Task ConvertFromRgbAsync()
    {
        try
        {
            var r = await _service.FromRgbAsync(Red, Green, Blue, CancellationToken.None);
            if (r.IsSuccess) Apply(r.Value!);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "RGB conversion failed."); }
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanApplyHex))]
    private async Task ApplyHexAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.FromHexAsync(HexValue, ct);
            if (!r.IsSuccess) { ReportFailure(r, "hex"); return; }
            _suppressAutoConvert = true;
            try { Red = r.Value!.Red; Green = r.Value.Green; Blue = r.Value.Blue; }
            finally { _suppressAutoConvert = false; }
            Apply(r.Value!);
            SetStatus(NotificationType.Success, "L.RgbColor.Status.HexApplied", r.Value!.Hex);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Apply hex failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanApplyHex() => !string.IsNullOrWhiteSpace(HexValue);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RandomColorAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.RandomAsync(ct);
            if (!r.IsSuccess) { ReportFailure(r, "random"); return; }
            _suppressAutoConvert = true;
            try { Red = r.Value!.Red; Green = r.Value.Green; Blue = r.Value.Blue; }
            finally { _suppressAutoConvert = false; }
            Apply(r.Value!);
            SetStatus(NotificationType.Info, "L.RgbColor.Status.Random");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Random color failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void CopyHex() => Copy(HexValue, "L.RgbColor.Status.HexCopied");
    [RelayCommand] private void CopyRgb() => Copy(RgbValue, "L.RgbColor.Status.RgbCopied");
    [RelayCommand] private void CopyHsl() => Copy(HslValue, "L.RgbColor.Status.HslCopied");

    private void Copy(string text, string successKey)
    {
        if (string.IsNullOrWhiteSpace(text)) { SetStatus(NotificationType.Warning, "L.RgbColor.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(text); SetStatus(NotificationType.Success, successKey); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        _suppressAutoConvert = true;
        try { Red = 0; Green = 0; Blue = 0; }
        finally { _suppressAutoConvert = false; }
        _ = ConvertFromRgbAsync();
        SetStatus(NotificationType.Info, "L.RgbColor.Status.Cleared");
    }

    private void Apply(ColorReport c)
    {
        HexValue = c.Hex;
        RgbValue = c.Rgb;
        HslValue = c.Hsl;
        ColorInfo = c.Description;
        PreviewColor = Color.FromRgb((byte)c.Red, (byte)c.Green, (byte)c.Blue);
        PreviewTextColor = c.IsLight ? Colors.Black : Colors.White;
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("RgbColor {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}