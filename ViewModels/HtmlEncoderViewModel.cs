using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// HTML Encoder/Decoder aracının sunum katmanı. İş mantığı <see cref="IHtmlEncoderService"/> içindedir;
/// hiçbir metin koda gömülü değildir ve hata durumunda istisna sızdırılmaz.
/// </summary>
public partial class HtmlEncoderViewModel : ObservableObject
{
    private readonly IHtmlEncoderService _htmlService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HtmlEncoderViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    /// <summary>Adlandırılmış varlık (&amp;amp;) veya sayısal referans (&amp;#38;).</summary>
    [ObservableProperty] private bool _useNamedEntities = true;

    /// <summary>ASCII dışı karakterler de kodlanır; çıktı saf ASCII olur.</summary>
    [ObservableProperty] private bool _encodeNonAscii;

    /// <summary>Çözmede tanınmayan/bozuk varlıklar hata olarak bildirilir.</summary>
    [ObservableProperty] private bool _strictEntities = true;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public HtmlEncoderViewModel(
        IHtmlEncoderService htmlService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HtmlEncoderViewModel> logger)
    {
        _htmlService = htmlService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Html.Status.Ready");
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Html.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            bool named = UseNamedEntities;
            bool nonAscii = EncodeNonAscii;

            ToolResult<string> result = await Task.Run(
                () => _htmlService.Encode(input, named, nonAscii), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Html encode completed: namedEntities={Named} encodeNonAscii={NonAscii} inputChars={InputChars} outputChars={OutputChars}",
                named, nonAscii, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Html.Status.Encoded", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Html encode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Html encode failed unexpectedly.");
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
            SetStatus(NotificationType.Warning, "L.Html.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            bool strict = StrictEntities;

            ToolResult<string> result = await Task.Run(() => _htmlService.Decode(input, strict), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Html decode completed: strictEntities={Strict} inputChars={InputChars} outputChars={OutputChars}",
                strict, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Html.Status.Decoded", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Html decode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Html decode failed unexpectedly.");
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
        SetStatus(NotificationType.Info, "L.Html.Status.Swapped");
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Html.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Html.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Html output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Html.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt|{_localizer.Get("L.Common.Filter.HtmlFiles")}|*.html",
                ".txt",
                "html-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Html.Status.Saved" : "L.Html.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Html output could not be saved.");
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
        SetStatus(NotificationType.Info, "L.Html.Status.Cleared");
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
            _logger.LogInformation("Html {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Html {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
