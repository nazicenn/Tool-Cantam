using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Kaçış biçiminin arayüzde gösterilecek hâli (görünen ad yerelleştirilir).</summary>
public sealed record UnicodeEscapeFormatOption(UnicodeEscapeFormat Format, string DisplayName);

/// <summary>
/// Unicode kaçış dönüştürücüsünün sunum katmanı. İş mantığı <see cref="IUnicodeConverterService"/>
/// içindedir; hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class UnicodeConverterViewModel : ObservableObject
{
    private readonly IUnicodeConverterService _unicodeService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<UnicodeConverterViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private UnicodeEscapeFormatOption _selectedFormat;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public UnicodeConverterViewModel(
        IUnicodeConverterService unicodeService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<UnicodeConverterViewModel> logger)
    {
        _unicodeService = unicodeService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        Formats = new[]
        {
            new UnicodeEscapeFormatOption(UnicodeEscapeFormat.Utf16Escape, _localizer.Get("L.Unicode.Format.Utf16")),
            new UnicodeEscapeFormatOption(UnicodeEscapeFormat.CodePointEscape, _localizer.Get("L.Unicode.Format.CodePoint")),
            new UnicodeEscapeFormatOption(UnicodeEscapeFormat.Utf8ByteEscape, _localizer.Get("L.Unicode.Format.Utf8Bytes")),
            new UnicodeEscapeFormatOption(UnicodeEscapeFormat.CodePointNotation, _localizer.Get("L.Unicode.Format.Notation"))
        };

        _selectedFormat = Formats[0];
        StatusText = _localizer.Get("L.Unicode.Status.Ready");
    }

    /// <summary>Seçilebilir kaçış biçimleri.</summary>
    public IReadOnlyList<UnicodeEscapeFormatOption> Formats { get; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EscapeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Unicode.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            UnicodeEscapeFormat format = SelectedFormat.Format;

            ToolResult<string> result = await Task.Run(() => _unicodeService.Escape(input, format), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "escape");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Unicode escape completed: format={Format} inputChars={InputChars} outputChars={OutputChars}",
                format, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Unicode.Status.Escaped", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Unicode escape cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unicode escape failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task UnescapeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Unicode.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _unicodeService.Unescape(input), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "unescape");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Unicode unescape completed: inputChars={InputChars} outputChars={OutputChars}",
                input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Unicode.Status.Unescaped", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Unicode unescape cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unicode unescape failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.Unicode.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Unicode.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Unicode.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unicode output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Unicode.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "unicode-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Unicode.Status.Saved" : "L.Unicode.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unicode output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Unicode.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(EscapeCancelCommand);
        CancelIfRunning(UnescapeCancelCommand);
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
            _logger.LogInformation("Unicode {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Unicode {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
