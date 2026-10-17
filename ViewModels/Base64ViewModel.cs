using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// Base64 Codec aracının sunum katmanı. İş mantığı <see cref="IBase64Service"/> içindedir;
/// bu sınıf komutları, durum metnini ve kullanıcı geri bildirimini yönetir.
/// Hiçbir metin koda gömülü değildir; hepsi <see cref="ILocalizerService"/> üzerinden çözülür.
/// </summary>
public partial class Base64ViewModel : ObservableObject
{
    private readonly IBase64Service _base64Service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<Base64ViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _outputText = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _useUrlSafeAlphabet;

    /// <summary>Girdi eksik olduğunda görünüm girdi alanına odaklanır.</summary>
    public event Action? RequestInputFocus;

    public Base64ViewModel(
        IBase64Service base64Service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<Base64ViewModel> logger)
    {
        _base64Service = base64Service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Base64.Status.Ready");
    }

    private Base64Variant Variant => UseUrlSafeAlphabet ? Base64Variant.UrlSafe : Base64Variant.Standard;

    // ---------------------------------------------------------------------
    // Metin işlemleri
    // ---------------------------------------------------------------------

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeTextAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Base64.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _base64Service.EncodeText(input, Variant), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode-text");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            // Girdi içeriği loglanmaz; yalnızca boyut ve sonuç.
            _logger.LogInformation("Base64 encode-text completed: variant={Variant} inputChars={InputChars} outputChars={OutputChars} elapsedMs={ElapsedMs}",
                Variant, input.Length, OutputText.Length, result.Elapsed.TotalMilliseconds);

            SetStatus(NotificationType.Success, "L.Base64.Status.Encoded", input.Length, OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Base64 encode-text cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base64 encode-text failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DecodeTextAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Base64.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        try
        {
            string input = InputText;
            ToolResult<string> result = await Task.Run(() => _base64Service.DecodeText(input, Variant), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode-text");
                return;
            }

            OutputText = result.Value ?? string.Empty;

            _logger.LogInformation("Base64 decode-text completed: variant={Variant} inputChars={InputChars} outputChars={OutputChars}",
                Variant, input.Length, OutputText.Length);

            SetStatus(NotificationType.Success, "L.Base64.Status.Decoded", OutputText.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Base64 decode-text cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base64 decode-text failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Base64.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputText);
            SetStatus(NotificationType.Success, "L.Base64.Status.Copied");
        }
        catch (Exception ex)
        {
            // Pano başka bir uygulama tarafından kilitli olabilir; uygulama çökmemeli.
            _logger.LogError(ex, "Base64 output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputText))
        {
            SetStatus(NotificationType.Warning, "L.Base64.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputText,
                BuildFilter("L.Common.Filter.TextFiles", "*.txt"),
                ".txt",
                "base64-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Base64.Status.Saved" : "L.Base64.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base64 output could not be saved.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // Dosya işlemleri (akış halinde, iptal edilebilir)
    // ---------------------------------------------------------------------

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task EncodeFileAsync(CancellationToken cancellationToken)
    {
        string? source = ResolveSourceFile();
        if (source is null)
            return;

        string? destination = _fileExportService.RequestSavePath(
            BuildFilter("L.Base64.Filter.Base64Files", "*.b64"),
            ".b64",
            Path.GetFileName(source) + ".b64");

        if (string.IsNullOrWhiteSpace(destination))
        {
            SetStatus(NotificationType.Info, "L.Base64.Status.SaveCancelled");
            return;
        }

        IsBusy = true;
        try
        {
            SetStatus(NotificationType.Info, "L.Base64.Status.EncodingFile");

            ToolResult<long> result = await _base64Service.EncodeFileAsync(source, destination, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "encode-file");
                return;
            }

            _logger.LogInformation("Base64 encode-file completed: source={Source} outputBytes={OutputBytes}", source, result.Value);
            SetStatus(NotificationType.Success, "L.Base64.Status.FileEncoded", result.Value, destination);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Base64 encode-file cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base64 encode-file failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DecodeFileAsync(CancellationToken cancellationToken)
    {
        string? source = ResolveSourceFile();
        if (source is null)
            return;

        string? destination = _fileExportService.RequestSavePath(
            BuildFilter("L.Common.Filter.AllFiles", "*.*"),
            string.Empty,
            Path.GetFileNameWithoutExtension(source) + ".decoded");

        if (string.IsNullOrWhiteSpace(destination))
        {
            SetStatus(NotificationType.Info, "L.Base64.Status.SaveCancelled");
            return;
        }

        IsBusy = true;
        try
        {
            SetStatus(NotificationType.Info, "L.Base64.Status.DecodingFile");

            ToolResult<long> result = await _base64Service.DecodeFileAsync(source, destination, cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode-file");
                return;
            }

            _logger.LogInformation("Base64 decode-file completed: source={Source} outputBytes={OutputBytes}", source, result.Value);
            SetStatus(NotificationType.Success, "L.Base64.Status.FileDecoded", result.Value, destination);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Base64 decode-file cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base64 decode-file failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ---------------------------------------------------------------------
    // Yardımcılar
    // ---------------------------------------------------------------------

    [RelayCommand]
    private void Clear()
    {
        CancelRunningOperations();

        InputText = string.Empty;
        OutputText = string.Empty;
        FilePath = string.Empty;
        SetStatus(NotificationType.Info, "L.Base64.Status.Cleared");
    }

    /// <summary>Devam eden bir işlem varsa iptal eder (kullanıcı TEMİZLE dediğinde).</summary>
    private void CancelRunningOperations()
    {
        CancelIfRunning(EncodeTextCancelCommand);
        CancelIfRunning(DecodeTextCancelCommand);
        CancelIfRunning(EncodeFileCancelCommand);
        CancelIfRunning(DecodeFileCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }

    private string? ResolveSourceFile()
    {
        if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            return FilePath;

        string? selected = _fileExportService.RequestOpenPath(
            BuildFilter("L.Common.Filter.AllFiles", "*.*"),
            _localizer.Get("L.Base64.Dialog.SelectFile"));

        if (string.IsNullOrWhiteSpace(selected))
            return null;

        FilePath = selected;
        return selected;
    }

    private string BuildFilter(string descriptionKey, string pattern) =>
        $"{_localizer.Get(descriptionKey)}|{pattern}";

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    /// <summary>
    /// Servis hatasını kullanıcıya yerelleştirilmiş metinle bildirir; teknik ayrıntı yalnızca loga gider.
    /// </summary>
    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("Base64 {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("Base64 {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
