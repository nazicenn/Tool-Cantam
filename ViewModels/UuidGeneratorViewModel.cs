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

/// <summary>UUID Generator sunum katmanı.</summary>
public partial class UuidGeneratorViewModel : ObservableObject
{
    private readonly IUuidGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<UuidGeneratorViewModel> _logger;

    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private UuidCase _case = UuidCase.Upper;
    [ObservableProperty] private bool _includeHyphens = true;
    [ObservableProperty] private bool _includeBraces;
    [ObservableProperty] private string _output = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public UuidGeneratorViewModel(
        IUuidGeneratorService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<UuidGeneratorViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.UuidGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.UuidGenerator.Limits", _service.MinCount, _service.MaxCount);
    public UuidCase[] Cases => new[] { UuidCase.Upper, UuidCase.Lower };

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new UuidOptions(Count, Case, IncludeHyphens, IncludeBraces);
            var result = await _service.GenerateAsync(opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "generate");
                Output = string.Empty;
                return;
            }

            var r = result.Value!;
            Output = r.Combined;

            _logger.LogInformation("UUID generated: count={Count} version={Version} elapsed={Ms:0}ms",
                r.Items.Count, r.Version, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.UuidGenerator.Status.Generated", r.Items.Count);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UUID generate failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(Output))
        {
            SetStatus(NotificationType.Warning, "L.UuidGenerator.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(Output);
            SetStatus(NotificationType.Success, "L.UuidGenerator.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveOutput()
    {
        if (string.IsNullOrWhiteSpace(Output))
        {
            SetStatus(NotificationType.Warning, "L.UuidGenerator.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.UuidGenerator.Filter.Text"), ".txt", "uuids");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, Output);
            SetStatus(NotificationType.Success, "L.UuidGenerator.Status.Saved", Path.GetFileName(path));
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
        Output = string.Empty;
        SetStatus(NotificationType.Info, "L.UuidGenerator.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("UuidGenerator {Op} failed: kind={Kind} code={Code}", op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}