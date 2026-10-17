using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Hex çıktı biçiminin arayüzde gösterilecek hâli (görünen ad yerelleştirilir).</summary>
public sealed record HexFormatOption(HexFormat Format, string DisplayName);

/// <summary>
/// Hex Encoder/Decoder aracının sunum katmanı. İş mantığı <see cref="IHexService"/> içindedir;
/// hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class HexViewModel : ObservableObject
{
    private readonly IHexService _hexService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HexViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private HexFormatOption _selectedFormat;
    [ObservableProperty] private bool _uppercaseHex = true;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public HexViewModel(
        IHexService hexService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HexViewModel> logger)
    {
        _hexService = hexService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Formats = new[]
        {
            new HexFormatOption(HexFormat.Compact, _localizer.Get("L.Hex.Format.Compact")),
            new HexFormatOption(HexFormat.Spaced, _localizer.Get("L.Hex.Format.Spaced")),
            new HexFormatOption(HexFormat.ColonSeparated, _localizer.Get("L.Hex.Format.ColonSeparated"))
        };

        _selectedFormat = Formats[0];
        StatusText = _localizer.Get("L.Hex.Status.Ready");
    }

    /// <summary>Seçilebilir çıktı biçimleri.</summary>
    public IReadOnlyList<HexFormatOption> Formats { get; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Hex.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            HexFormat format = SelectedFormat.Format;
            bool uppercase = UppercaseHex;

            ToolResult<string> result = await Task.Run(
                () => _hexService.Encode(input, format, uppercase), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Hex encode completed: format={Format} uppercase={Uppercase} inputChars={InputChars} outputChars={OutputChars}",
                format, uppercase, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Hex.Status.Encoded", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hex encode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hex encode failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.Hex.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _hexService.Decode(input), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Hex decode completed: inputChars={InputChars} outputChars={OutputChars}",
                input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Hex.Status.Decoded", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hex decode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hex decode failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.Hex.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Hex.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Hex.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hex output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Hex.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "hex-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Hex.Status.Saved" : "L.Hex.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hex output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Hex.Status.Cleared");
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
            _logger.LogInformation("Hex {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Hex {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
