using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class CssGradientGeneratorViewModel : ObservableObject
{
    private readonly ICssGradientGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CssGradientGeneratorViewModel> _logger;

    [ObservableProperty] private string _color1 = "#FF5733";
    [ObservableProperty] private string _color2 = "#33FF57";
    [ObservableProperty] private GradientDirection _direction = GradientDirection.ToRight;
    [ObservableProperty] private string _gradientCss = string.Empty;
    [ObservableProperty] private string _previewColor = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public CssGradientGeneratorViewModel(ICssGradientGeneratorService service, IFileExportService fe, ILocalizerService loc, ILogger<CssGradientGeneratorViewModel> log)
    {
        _service = service; _fileExportService = fe; _localizer = loc; _logger = log;
        StatusText = _localizer.Get("L.CssGradientGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Get("L.CssGradientGenerator.Limits");
    public GradientDirection[] Directions => new[]
    {
        GradientDirection.ToRight, GradientDirection.ToLeft,
        GradientDirection.ToBottom, GradientDirection.ToTop,
        GradientDirection.ToBottomRight, GradientDirection.ToTopLeft,
    };

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var r = await _service.GenerateAsync(new GradientOptions(Color1, Color2, Direction), ct);
            if (!r.IsSuccess) { ReportFailure(r, "gen"); GradientCss = string.Empty; PreviewColor = string.Empty; return; }
            GradientCss = r.Value!.Css;
            PreviewColor = r.Value.Css;
            _logger.LogInformation("CSS gradient generated: {Dir}", r.Value.Direction);
            SetStatus(NotificationType.Success, "L.CssGradientGenerator.Status.Generated");
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "Gradient gen failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyCss()
    {
        if (string.IsNullOrWhiteSpace(GradientCss)) { SetStatus(NotificationType.Warning, "L.CssGradientGenerator.Validation.NothingToCopy"); return; }
        try { _fileExportService.CopyToClipboard(GradientCss); SetStatus(NotificationType.Success, "L.CssGradientGenerator.Status.Copied"); }
        catch (Exception ex) { _logger.LogError(ex, "Copy failed."); SetStatus(NotificationType.Error, "L.Common.Error.Clipboard"); }
    }

    [RelayCommand] private void LoadSample() { Color1 = "#FF5733"; Color2 = "#33FF57"; Direction = GradientDirection.ToRight; GenerateCommand.Execute(null); }
    [RelayCommand] private void Clear() { Color1 = "#FF5733"; Color2 = "#33FF57"; Direction = GradientDirection.ToRight; GradientCss = string.Empty; PreviewColor = string.Empty; SetStatus(NotificationType.Info, "L.CssGradientGenerator.Status.Cleared"); }

    private void SetStatus(NotificationType t, string k, params object?[] a) { StatusType = t; StatusText = _localizer.Format(k, a); }
    private void ReportFailure<T>(ToolResult<T> r, string op) { _logger.LogWarning("CssGradientGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode); SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected"); }
}