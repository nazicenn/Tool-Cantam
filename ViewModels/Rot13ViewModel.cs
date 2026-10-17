using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// ROT-N (harf döndürme) aracının sunum katmanı. İş mantığı <see cref="IRot13Service"/> içindedir.
/// <para>
/// Araç, bunun bir şifreleme yöntemi olmadığını arayüzde açıkça belirtir.
/// </para>
/// </summary>
public partial class Rot13ViewModel : ObservableObject
{
    /// <summary>Kaydırma miktarının sınırları; 13 klasik ROT13'tür.</summary>
    public const int MinimumShift = 1;

    public const int MaximumShift = AlphabetLength - 1;

    private const int AlphabetLength = 26;

    public const int DefaultShift = 13;

    private readonly IRot13Service _rotService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<Rot13ViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _shift = DefaultShift;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public Rot13ViewModel(
        IRot13Service rotService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<Rot13ViewModel> logger)
    {
        _rotService = rotService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Rot13.Status.Ready");
    }

    /// <summary>Klasik ROT13 (13) seçili mi; bu durumda kodlama ve çözme aynı sonucu verir.</summary>
    public bool IsSelfInverse => Math.Clamp(Shift, MinimumShift, MaximumShift) == DefaultShift;

    partial void OnShiftChanged(int value) => OnPropertyChanged(nameof(IsSelfInverse));

    [RelayCommand(IncludeCancelCommand = true)]
    private Task EncodeAsync(CancellationToken cancellationToken) =>
        RotateAsync(CurrentShift, "L.Rot13.Status.Encoded", "encode", cancellationToken);

    [RelayCommand(IncludeCancelCommand = true)]
    private Task DecodeAsync(CancellationToken cancellationToken) =>
        RotateAsync(-CurrentShift, "L.Rot13.Status.Decoded", "decode", cancellationToken);

    private int CurrentShift => Math.Clamp(Shift, MinimumShift, MaximumShift);

    private async Task RotateAsync(int shift, string statusKey, string operation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Rot13.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _rotService.Rotate(input, shift), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, operation);
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Rot {Operation} completed: shift={Shift} inputChars={InputChars} outputChars={OutputChars}",
                operation, shift, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, statusKey, input.Length, Math.Abs(shift));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Rot {Operation} cancelled by the user.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rot {Operation} failed unexpectedly.", operation);
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
        SetStatus(NotificationType.Info, "L.Rot13.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Rot13.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Rot13.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rot output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Rot13.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "rot-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Rot13.Status.Saved" : "L.Rot13.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rot output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Rot13.Status.Cleared");
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
            _logger.LogInformation("Rot {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Rot {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
