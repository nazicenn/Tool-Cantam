using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>Anahtar biçiminin arayüzde gösterilecek hâli.</summary>
public sealed record HmacKeyFormatOption(HmacKeyFormat Format, string DisplayName);

/// <summary>
/// HMAC Generator aracının sunum katmanı. İş mantığı <see cref="IHmacService"/> içindedir.
/// <para>
/// Anahtar hiçbir zaman loglanmaz; arayüzde varsayılan olarak maskelenir. Beklenen MAC girilmişse
/// sonuç <see cref="CryptographicOperations.FixedTimeEquals"/> ile sabit zamanda karşılaştırılır.
/// </para>
/// </summary>
public partial class HmacGeneratorViewModel : ObservableObject
{
    private readonly IHmacService _hmacService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<HmacGeneratorViewModel> _logger;

    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private string _secretKey = string.Empty;
    [ObservableProperty] private string _outputHash = string.Empty;
    [ObservableProperty] private string _expectedMac = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showKey;
    [ObservableProperty] private HashAlgorithmDescriptor? _selectedAlgorithm;
    [ObservableProperty] private HmacKeyFormatOption _selectedKeyFormat;
    [ObservableProperty] private HashOutputFormatOption _selectedOutputFormat;
    [ObservableProperty] private HashMatchState _verificationState = HashMatchState.NotChecked;
    [ObservableProperty] private string _verificationText = string.Empty;

    /// <summary>Anahtar eksik olduğunda görünüm anahtar alanına odaklanır.</summary>
    public event Action? RequestKeyFocus;

    public HmacGeneratorViewModel(
        IHmacService hmacService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<HmacGeneratorViewModel> logger)
    {
        _hmacService = hmacService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        KeyFormats = new[]
        {
            new HmacKeyFormatOption(HmacKeyFormat.Text, _localizer.Get("L.Hmac.KeyFormat.Text")),
            new HmacKeyFormatOption(HmacKeyFormat.Hex, _localizer.Get("L.Hmac.KeyFormat.Hex"))
        };

        OutputFormats = new[]
        {
            new HashOutputFormatOption(HashOutputFormat.HexUppercase, _localizer.Get("L.Hash.Format.HexUpper")),
            new HashOutputFormatOption(HashOutputFormat.HexLowercase, _localizer.Get("L.Hash.Format.HexLower")),
            new HashOutputFormatOption(HashOutputFormat.Base64, _localizer.Get("L.Hash.Format.Base64"))
        };

        _selectedKeyFormat = KeyFormats[0];
        _selectedOutputFormat = OutputFormats[0];
        _selectedAlgorithm = DefaultAlgorithm();
        StatusText = _localizer.Get("L.Hmac.Status.Ready");
    }

    /// <summary>Bu makinede kullanılabilen HMAC algoritmaları.</summary>
    public IReadOnlyList<HashAlgorithmDescriptor> Algorithms => _hmacService.Algorithms;

    public IReadOnlyList<HmacKeyFormatOption> KeyFormats { get; }

    public IReadOnlyList<HashOutputFormatOption> OutputFormats { get; }

    /// <summary>Seçili algoritma zayıf mı (HMAC-MD5, HMAC-SHA1).</summary>
    public bool IsSelectedAlgorithmWeak => SelectedAlgorithm?.IsWeak == true;

    partial void OnSelectedAlgorithmChanged(HashAlgorithmDescriptor? value) =>
        OnPropertyChanged(nameof(IsSelectedAlgorithmWeak));

    private HashAlgorithmDescriptor? DefaultAlgorithm()
    {
        foreach (HashAlgorithmDescriptor descriptor in _hmacService.Algorithms)
        {
            if (descriptor.Kind == HashAlgorithmKind.Sha256)
                return descriptor;
        }

        return _hmacService.Algorithms.Count > 0 ? _hmacService.Algorithms[0] : null;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SecretKey))
        {
            RequestKeyFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Hmac.Validation.EmptyKey");
            return;
        }

        HashAlgorithmDescriptor? descriptor = SelectedAlgorithm;

        if (descriptor is null)
        {
            SetStatus(NotificationType.Warning, "L.Hmac.Validation.NoAlgorithm");
            return;
        }

        string message = InputText;
        string key = SecretKey;
        HmacKeyFormat keyFormat = SelectedKeyFormat.Format;
        HashOutputFormat outputFormat = SelectedOutputFormat.Format;

        IsBusy = true;
        try
        {
            ToolResult<byte[]> result = await Task.Run(
                () => _hmacService.Compute(message, key, keyFormat, descriptor.Kind), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "hmac");
                return;
            }

            byte[] mac = result.Value!;

            try
            {
                OutputHash = FormatMac(mac, outputFormat);

                // Anahtar ve mesaj içeriği loglanmaz.
                _logger.LogInformation(
                    "HMAC computed: algorithm={Algorithm} messageChars={MessageChars} keyFormat={KeyFormat} keyBytes={KeyBytes} output={Output}",
                    descriptor.DisplayName, message.Length, keyFormat, key.Length, outputFormat);

                SetStatus(NotificationType.Success, "L.Hmac.Status.Computed", descriptor.DisplayName);
                UpdateVerification(mac);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mac);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HMAC computation cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HMAC computation failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Beklenen MAC girilmişse sonucu sabit zamanda karşılaştırır. Düz metin karşılaştırması
    /// zamanlama sızıntısı ürettiği için <see cref="CryptographicOperations.FixedTimeEquals"/> kullanılır.
    /// </summary>
    private void UpdateVerification(byte[] mac)
    {
        if (string.IsNullOrWhiteSpace(ExpectedMac))
        {
            VerificationState = HashMatchState.NotChecked;
            VerificationText = string.Empty;
            return;
        }

        if (!_hmacService.TryParseMac(ExpectedMac, out byte[] expected))
        {
            VerificationState = HashMatchState.NotComparable;
            VerificationText = _localizer.Get("L.Hmac.Verify.Invalid");
            return;
        }

        bool isMatch = expected.Length == mac.Length && CryptographicOperations.FixedTimeEquals(expected, mac);

        VerificationState = isMatch ? HashMatchState.Matched : HashMatchState.Mismatched;
        VerificationText = _localizer.Get(isMatch ? "L.Hmac.Verify.Matched" : "L.Hmac.Verify.Mismatched");

        if (!isMatch)
            _logger.LogInformation("HMAC verification did not match. expectedBytes={Expected} actualBytes={Actual}", expected.Length, mac.Length);
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (string.IsNullOrWhiteSpace(OutputHash))
        {
            SetStatus(NotificationType.Warning, "L.Hmac.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(OutputHash);
            SetStatus(NotificationType.Success, "L.Hmac.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HMAC output could not be copied to the clipboard.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveHashAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputHash))
        {
            SetStatus(NotificationType.Warning, "L.Hmac.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                OutputHash,
                $"{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".txt",
                "hmac-output");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Hmac.Status.Saved" : "L.Hmac.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HMAC output could not be saved.");
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
        SecretKey = string.Empty;
        OutputHash = string.Empty;
        ExpectedMac = string.Empty;
        VerificationState = HashMatchState.NotChecked;
        VerificationText = string.Empty;
        ShowKey = false;
        SetStatus(NotificationType.Info, "L.Hmac.Status.Cleared");
    }

    private void CancelRunningOperations()
    {
        CancelIfRunning(GenerateCancelCommand);
    }

    private static void CancelIfRunning(System.Windows.Input.ICommand? command)
    {
        if (command is not null && command.CanExecute(null))
            command.Execute(null);
    }

    private static string FormatMac(ReadOnlySpan<byte> mac, HashOutputFormat format) => format switch
    {
        HashOutputFormat.HexLowercase => HexFormatting.ToHexLower(mac),
        HashOutputFormat.Base64 => Convert.ToBase64String(mac),
        _ => HexFormatting.ToHexUpper(mac)
    };

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        if (result.ErrorKind == ToolErrorKind.Cancelled)
        {
            _logger.LogInformation("HMAC {Operation} cancelled.", operation);
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
            return;
        }

        _logger.LogWarning("HMAC {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}
