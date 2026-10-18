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

public partial class ColorPickerViewModel : ObservableObject
{
    private readonly IColorPickerService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<ColorPickerViewModel> _logger;

    [ObservableProperty] private Color _selectedColor = Colors.DodgerBlue;
    [ObservableProperty] private string _hexColor = "#1E90FF";
    [ObservableProperty] private string _rgbColor = "rgb(30, 144, 255)";
    [ObservableProperty] private string _hslColor = "hsl(210, 100%, 56%)";
    [ObservableProperty] private byte _red = 30;
    [ObservableProperty] private byte _green = 144;
    [ObservableProperty] private byte _blue = 255;
    [ObservableProperty] private double _hue;
    [ObservableProperty] private double _saturation;
    [ObservableProperty] private double _lightness;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    private bool _suppressAutoConvert;

    public ColorPickerViewModel(IColorPickerService service, IFileExportService fe, ILocalizerService loc, ILogger<ColorPickerViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.ColorPicker.Status.Ready");
        _ = ConvertFromRgbAsync();
    }

    public string LimitsText => _localizer.Format("L.ColorPicker.Limits", _service.MaxHexLength);

    partial void OnRedChanged(byte value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }
    partial void OnGreenChanged(byte value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }
    partial void OnBlueChanged(byte value) { if (!_suppressAutoConvert) _ = ConvertFromRgbAsync(); }

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
            var r = await _service.FromHexAsync(HexColor, ct);
            if (!r.IsSuccess) { ReportFailure(r, "hex"); return; }
            _suppressAutoConvert = true;
            try { Red = (byte)r.Value!.Red; Green = (byte)r.Value.Green; Blue = (byte)r.Value.Blue; }
            finally { _suppressAutoConvert = false; }
            Apply(r.Value!);
            SetStatus(NotificationType.Success, "L.ColorPicker.Status.HexApplied", r.Value!.Hex);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Apply hex failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanApplyHex() => !string.IsNullOrWhiteSpace(HexColor);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RandomColorAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.RandomAsync(ct);
            if (!r.IsSuccess) { ReportFailure(r, "random"); return; }
            _suppressAutoConvert = true;
            try { Red = (byte)r.Value!.Red; Green = (byte)r.Value.Green; Blue = (byte)r.Value.Blue; }
            finally { _suppressAutoConvert = false; }
            Apply(r.Value!);
            SetStatus(NotificationType.Info, "L.ColorPicker.Status.Random");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Random failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void CopyHex() => Copy(HexColor, "L.ColorPicker.Status.CopiedHex");
    [RelayCommand] private void CopyRgb() => Copy(RgbColor, "L.ColorPicker.Status.CopiedRgb");
    [RelayCommand] private void CopyHsl() => Copy(HslColor, "L.ColorPicker.Status.CopiedHsl");

    private void Copy(string text, string successKey)
    {
        if (string.IsNullOrWhiteSpace(text)) { SetStatus(NotificationType.Warning, "L.ColorPicker.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(text); SetStatus(NotificationType.Success, successKey); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand]
    private void Clear()
    {
        _suppressAutoConvert = true;
        try { Red = 30; Green = 144; Blue = 255; }
        finally { _suppressAutoConvert = false; }
        _ = ConvertFromRgbAsync();
        SetStatus(NotificationType.Info, "L.ColorPicker.Status.Cleared");
    }

    private void Apply(ColorPickerReport c)
    {
        HexColor = c.Hex;
        RgbColor = c.Rgb;
        HslColor = c.Hsl;
        Hue = Math.Round(c.Hue, 1);
        Saturation = Math.Round(c.Saturation, 1);
        Lightness = Math.Round(c.Lightness, 1);
        SelectedColor = Color.FromRgb((byte)c.Red, (byte)c.Green, (byte)c.Blue);
    }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("ColorPicker {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}