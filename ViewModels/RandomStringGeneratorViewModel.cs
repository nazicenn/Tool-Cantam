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

/// <summary>Random String Generator sunum katmanı.</summary>
public partial class RandomStringGeneratorViewModel : ObservableObject
{
    private readonly IRandomStringGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<RandomStringGeneratorViewModel> _logger;

    [ObservableProperty] private int _length = 16;
    [ObservableProperty] private int _count = 1;
    [ObservableProperty] private bool _includeUppercase = true;
    [ObservableProperty] private bool _includeLowercase = true;
    [ObservableProperty] private bool _includeDigits = true;
    [ObservableProperty] private bool _includeSymbols;
    [ObservableProperty] private bool _excludeSimilar;
    [ObservableProperty] private bool _ensureEachType;
    [ObservableProperty] private string _output = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public RandomStringGeneratorViewModel(
        IRandomStringGeneratorService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<RandomStringGeneratorViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.RandomStringGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.RandomStringGenerator.Limits",
        _service.MinLength, _service.MaxLength, _service.MinCount, _service.MaxCount);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new RandomStringOptions(
                Length, Count,
                IncludeUppercase, IncludeLowercase, IncludeDigits, IncludeSymbols,
                ExcludeSimilar, EnsureEachType);

            var result = await _service.GenerateAsync(opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "generate");
                Output = string.Empty;
                return;
            }

            var r = result.Value!;
            Output = r.Combined;

            _logger.LogInformation("Random strings: count={Count} length={Length} entropy={Entropy} elapsed={Ms:0}ms",
                r.Items.Count, Length, r.EntropyBits, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.RandomStringGenerator.Status.Generated",
                r.Items.Count, r.EntropyBits);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Random string generate failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(Output))
        {
            SetStatus(NotificationType.Warning, "L.RandomStringGenerator.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(Output);
            SetStatus(NotificationType.Success, "L.RandomStringGenerator.Status.Copied");
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
            SetStatus(NotificationType.Warning, "L.RandomStringGenerator.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.RandomStringGenerator.Filter.Text"), ".txt", "random-strings");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, Output);
            SetStatus(NotificationType.Success, "L.RandomStringGenerator.Status.Saved", Path.GetFileName(path));
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
        SetStatus(NotificationType.Info, "L.RandomStringGenerator.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("RandomStringGenerator {Op} failed: kind={Kind} code={Code}",
            op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}