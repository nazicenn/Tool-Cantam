using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// Case Converter aracının sunum katmanı. İş mantığı <see cref="ICaseConverterService"/> içindedir.
/// Tüm dönüşümler async, iptal edilebilir; aynı anda tek işlem çalışır.
/// </summary>
public partial class CaseConverterViewModel : ObservableObject
{
    private readonly ICaseConverterService _caseService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<CaseConverterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public event Action? RequestInputFocus;

    public CaseConverterViewModel(
        ICaseConverterService caseService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<CaseConverterViewModel> logger)
    {
        _caseService = caseService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Case.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.Case.Limits", _caseService.MaxTextLength);

    // ★ Her komut kendi CancellationToken parametresini alır
    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToUpperAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Upper, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToLowerAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Lower, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToTitleAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Title, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToSentenceAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Sentence, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToCamelAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Camel, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToPascalAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Pascal, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToSnakeAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Snake, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToKebabAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Kebab, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToAlternatingAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Alternating, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ToInvertAsync(CancellationToken ct) => ConvertInternalAsync(CaseConversionMode.Invert, ct);

    private async Task ConvertInternalAsync(CaseConversionMode mode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Case.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;

            ToolResult<string> result = await Task.Run(
                () => _caseService.Convert(input, mode), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, mode.ToString());
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Case convert completed: mode={Mode} inputChars={InputChars} outputChars={OutputChars}",
                mode, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Case.Status.Converted", mode.ToString(), input.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Case convert cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Case convert failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        SetStatus(NotificationType.Info, "L.Case.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Case.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Case.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Case output could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Case.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "case-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Case.Status.Saved" : "L.Case.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Case output could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        InputText = string.Empty;
        OutputText = string.Empty;
        SetStatus(NotificationType.Info, "L.Case.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ToUpperCancelCommand);
        CancelIfRunning(ToLowerCancelCommand);
        CancelIfRunning(ToTitleCancelCommand);
        CancelIfRunning(ToSentenceCancelCommand);
        CancelIfRunning(ToCamelCancelCommand);
        CancelIfRunning(ToPascalCancelCommand);
        CancelIfRunning(ToSnakeCancelCommand);
        CancelIfRunning(ToKebabCancelCommand);
        CancelIfRunning(ToAlternatingCancelCommand);
        CancelIfRunning(ToInvertCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("Case {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Case {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}