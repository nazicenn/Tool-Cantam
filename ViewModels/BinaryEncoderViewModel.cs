using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>İkili gösterim biçiminin arayüzde gösterilecek hâli (görünen ad yerelleştirilir).</summary>
public sealed record BinaryFormatOption(BinaryFormat Format, string DisplayName);

/// <summary>
/// Binary Encoder/Decoder aracının sunum katmanı. İş mantığı <see cref="IBinaryService"/> içindedir;
/// hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class BinaryEncoderViewModel : ObservableObject
{
    private readonly IBinaryService _binaryService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<BinaryEncoderViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private BinaryFormatOption _selectedFormat;
    [ObservableProperty] private bool _includeBytePrefix;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public BinaryEncoderViewModel(
        IBinaryService binaryService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<BinaryEncoderViewModel> logger)
    {
        _binaryService = binaryService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Formats = new[]
        {
            new BinaryFormatOption(BinaryFormat.Compact, _localizer.Get("L.Binary.Format.Compact")),
            new BinaryFormatOption(BinaryFormat.Spaced, _localizer.Get("L.Binary.Format.Spaced"))
        };

        _selectedFormat = Formats[0];
        StatusText = _localizer.Get("L.Binary.Status.Ready");
    }

    /// <summary>Seçilebilir gösterim biçimleri.</summary>
    public IReadOnlyList<BinaryFormatOption> Formats { get; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Binary.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            BinaryFormat format = SelectedFormat.Format;
            bool prefix = IncludeBytePrefix;

            ToolResult<string> result = await Task.Run(
                () => _binaryService.Encode(input, format, prefix), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Binary encode completed: format={Format} prefix={Prefix} inputChars={InputChars} outputChars={OutputChars}",
                format, prefix, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Binary.Status.Encoded", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Binary encode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary encode failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.Binary.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _binaryService.Decode(input), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Binary decode completed: inputChars={InputChars} outputChars={OutputChars}",
                input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Binary.Status.Decoded", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Binary decode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary decode failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.Binary.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Binary.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Binary.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Binary.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "binary-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Binary.Status.Saved" : "L.Binary.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Binary.Status.Cleared");
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
            _logger.LogInformation("Binary {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Binary {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
