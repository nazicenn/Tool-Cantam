using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Kodlama modunun arayüzde gösterilecek hâli (görünen ad yerelleştirilir).</summary>
public sealed record UrlModeOption(UrlEncodingMode Mode, string DisplayName);

/// <summary>
/// URL Encoder/Decoder aracının sunum katmanı. İş mantığı <see cref="IUrlService"/> içindedir;
/// hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class UrlViewModel : ObservableObject
{
    private readonly IUrlService _urlService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<UrlViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private UrlModeOption _selectedMode;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public UrlViewModel(
        IUrlService urlService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<UrlViewModel> logger)
    {
        _urlService = urlService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Modes = new[]
        {
            new UrlModeOption(UrlEncodingMode.Component, _localizer.Get("L.Url.Mode.Component")),
            new UrlModeOption(UrlEncodingMode.Form, _localizer.Get("L.Url.Mode.Form")),
            new UrlModeOption(UrlEncodingMode.FullUrl, _localizer.Get("L.Url.Mode.FullUrl"))
        };

        _selectedMode = Modes[0];
        StatusText = _localizer.Get("L.Url.Status.Ready");
    }

    /// <summary>Seçilebilir kodlama modları.</summary>
    public IReadOnlyList<UrlModeOption> Modes { get; }

    private UrlEncodingMode Mode => SelectedMode.Mode;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Url.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _urlService.Encode(input, Mode), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Url encode completed: mode={Mode} inputChars={InputChars} outputChars={OutputChars}",
                Mode, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Url.Status.Encoded", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Url encode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Url encode failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DecodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Url.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _urlService.Decode(input, Mode), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Url decode completed: mode={Mode} inputChars={InputChars} outputChars={OutputChars}",
                Mode, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Url.Status.Decoded", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Url decode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Url decode failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.Url.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Url.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Url.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Url output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Url.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "url-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Url.Status.Saved" : "L.Url.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Url output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Url.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(EncodeCancelCommand);
        CancelIfRunning(DecodeCancelCommand);
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
            _logger.LogInformation("Url {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Url {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
