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

/// <summary>Slug Generator sunum katmanı.</summary>
public partial class SlugGeneratorViewModel : ObservableObject
{
    private readonly ISlugGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<SlugGeneratorViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private bool _toLowercase = true;
    [ObservableProperty] private char _separator = '-';
    [ObservableProperty] private int _maxLength = 80;
    [ObservableProperty] private bool _allowUnicode;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public SlugGeneratorViewModel(
        ISlugGeneratorService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<SlugGeneratorViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        StatusText = _localizer.Get("L.SlugGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.SlugGenerator.Limits",
        _service.MaxInputLength / (1024 * 1024), _service.MaxSlugLength);

    public char[] Separators => new[] { '-', '_', '.' };

    partial void OnInputTextChanged(string value) => GenerateCommand.NotifyCanExecuteChanged();

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            var opts = new SlugOptions(ToLowercase, Separator, MaxLength, AllowUnicode);
            var result = await _service.GenerateAsync(InputText, opts, cancellationToken);
            if (!result.IsSuccess)
            {
                ReportFailure(result, "generate");
                OutputText = string.Empty;
                return;
            }

            var r = result.Value!;
            OutputText = r.Slug;

            _logger.LogInformation("Slug generated: in={In} out={Out} elapsed={Ms:0}ms",
                r.OriginalLength, r.SlugLength, r.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.SlugGenerator.Status.Generated", r.SlugLength);
        }
        catch (OperationCanceledException)
        {
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slug generate failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally { IsBusy = false; }
    }

    private bool CanGenerate() => !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.SlugGenerator.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.SlugGenerator.Status.Copied");
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
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.SlugGenerator.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(
            _localizer.Get("L.SlugGenerator.Filter.Text"), ".txt", "slug");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllText(path, OutputText);
            SetStatus(NotificationType.Success, "L.SlugGenerator.Status.Saved", Path.GetFileName(path));
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
        InputText = string.Empty;
        OutputText = string.Empty;
        SetStatus(NotificationType.Info, "L.SlugGenerator.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string key, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(key, args);
    }

    private void ReportFailure<T>(ToolResult<T> result, string op)
    {
        _logger.LogWarning("SlugGenerator {Op} failed: kind={Kind} code={Code}",
            op, result.ErrorKind, result.ErrorCode);
        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}