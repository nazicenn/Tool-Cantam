using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Kod biçiminin arayüzde gösterilecek hâli (görünen ad yerelleştirilir).</summary>
public sealed record CharacterCodeFormatOption(CharacterCodeFormat Format, string DisplayName);

/// <summary>
/// Karakter kodu dönüştürücüsünün sunum katmanı. İş mantığı <see cref="IAsciiConverterService"/>
/// içindedir; hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class AsciiConverterViewModel : ObservableObject
{
    private readonly IAsciiConverterService _characterCodeService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<AsciiConverterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private CharacterCodeFormatOption _selectedFormat;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public AsciiConverterViewModel(
        IAsciiConverterService characterCodeService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<AsciiConverterViewModel> logger)
    {
        _characterCodeService = characterCodeService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Formats = new[]
        {
            new CharacterCodeFormatOption(CharacterCodeFormat.Decimal, _localizer.Get("L.CharacterCodes.Format.Decimal")),
            new CharacterCodeFormatOption(CharacterCodeFormat.Hexadecimal, _localizer.Get("L.CharacterCodes.Format.Hexadecimal"))
        };

        _selectedFormat = Formats[0];
        StatusText = _localizer.Get("L.CharacterCodes.Status.Ready");
    }

    /// <summary>Seçilebilir kod biçimleri.</summary>
    public IReadOnlyList<CharacterCodeFormatOption> Formats { get; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ToCodesAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.CharacterCodes.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            CharacterCodeFormat format = SelectedFormat.Format;

            ToolResult<string> result = await Task.Run(() => _characterCodeService.ToCodes(input, format), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "to-codes");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Character codes created: format={Format} inputChars={InputChars} outputChars={OutputChars}",
                format, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.CharacterCodes.Status.ConvertedToCodes", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Character code conversion cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Character code conversion failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ToTextAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.CharacterCodes.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            CharacterCodeFormat format = SelectedFormat.Format;

            ToolResult<string> result = await Task.Run(() => _characterCodeService.ToText(input, format), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "to-text");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Character codes read: format={Format} inputChars={InputChars} outputChars={OutputChars}",
                format, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.CharacterCodes.Status.ConvertedToText", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Character code conversion cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Character code conversion failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.CharacterCodes.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.CharacterCodes.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.CharacterCodes.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Character code output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.CharacterCodes.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "character-codes");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.CharacterCodes.Status.Saved" : "L.CharacterCodes.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Character code output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.CharacterCodes.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(ToCodesCancelCommand);
        CancelIfRunning(ToTextCancelCommand);
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
            _logger.LogInformation("Character code {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Character code {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
