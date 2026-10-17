using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// String Reverser aracının sunum katmanı. İş mantığı <see cref="IStringReverserService"/> içindedir.
/// 5 farklı ters çevirme modu; tümü async, iptal edilebilir.
/// </summary>
public partial class StringReverserViewModel : ObservableObject
{
    private readonly IStringReverserService _reverserService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<StringReverserViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public event Action? RequestInputFocus;

    public StringReverserViewModel(
        IStringReverserService reverserService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<StringReverserViewModel> logger)
    {
        _reverserService = reverserService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.StringReverser.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.StringReverser.Limits", _reverserService.MaxTextLength);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ReverseAllAsync(CancellationToken ct) => ConvertInternalAsync(StringReverseMode.ReverseAll, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ReverseWordsAsync(CancellationToken ct) => ConvertInternalAsync(StringReverseMode.ReverseWords, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ReverseEachWordAsync(CancellationToken ct) => ConvertInternalAsync(StringReverseMode.ReverseEachWord, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ReverseLinesAsync(CancellationToken ct) => ConvertInternalAsync(StringReverseMode.ReverseLines, ct);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ReverseLinesAndContentAsync(CancellationToken ct) => ConvertInternalAsync(StringReverseMode.ReverseLinesAndContent, ct);

    private async Task ConvertInternalAsync(StringReverseMode mode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.StringReverser.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;

            ToolResult<string> result = await Task.Run(
                () => _reverserService.Reverse(input, mode), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, mode.ToString());
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("String reverse completed: mode={Mode} inputChars={InputChars} outputChars={OutputChars}",
                mode, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.StringReverser.Status.Reversed", mode.ToString(), input.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("String reverse cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "String reverse failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.StringReverser.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.StringReverser.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.StringReverser.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "String reverse output could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.StringReverser.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "reversed-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.StringReverser.Status.Saved" : "L.StringReverser.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "String reverse output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.StringReverser.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ReverseAllCancelCommand);
        CancelIfRunning(ReverseWordsCancelCommand);
        CancelIfRunning(ReverseEachWordCancelCommand);
        CancelIfRunning(ReverseLinesCancelCommand);
        CancelIfRunning(ReverseLinesAndContentCancelCommand);
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
            _logger.LogInformation("String reverse {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("String reverse {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}