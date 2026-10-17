using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// JWT Decoder aracının sunum katmanı. İş mantığı <see cref="IJwtService"/> içindedir.
/// Sadece decode yapar, doğrulama yapmaz; süre dolumu kontrolü bilgilendirme amaçlıdır.
/// </summary>
public partial class JwtViewModel : ObservableObject
{
    private readonly IJwtService _jwtService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<JwtViewModel> _logger;

    [ObservableProperty] private string _jwtInput = string.Empty;
    [ObservableProperty] private string _decodedHeader = string.Empty;
    [ObservableProperty] private string _decodedPayload = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasResult;

    [ObservableProperty] private string _algorithm = "-";
    [ObservableProperty] private string _tokenType = "-";
    [ObservableProperty] private string _expiresAt = "-";
    [ObservableProperty] private string _expiryStatus = string.Empty;
    [ObservableProperty] private NotificationType _expiryStatusType = NotificationType.Info;

    public event Action? RequestInputFocus;

    public JwtViewModel(
        IJwtService jwtService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<JwtViewModel> logger)
    {
        _jwtService = jwtService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Jwt.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.Jwt.Limits", _jwtService.MaxTokenLength);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DecodeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(JwtInput))
        {
            RequestInputFocus?.Invoke();
            SetStatus(NotificationType.Warning, "L.Jwt.Validation.EmptyInput");
            return;
        }

        IsBusy = true;
        ClearResults();
        HasResult = false;

        try
        {
            string input = JwtInput;
            ToolResult<JwtReport> result = await Task.Run(
                () => _jwtService.Decode(input), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "decode");
                return;
            }

            var report = result.Value!;

            DecodedHeader = report.Header;
            DecodedPayload = report.Payload;
            Algorithm = report.Algorithm;
            TokenType = report.TokenType;

            if (report.Expiration.HasValue)
            {
                ExpiresAt = report.Expiration.Value.ToString("yyyy-MM-dd HH:mm:ss UTC");

                if (report.IsExpired)
                {
                    ExpiryStatus = _localizer.Get("L.Jwt.Expiry.Expired");
                    ExpiryStatusType = NotificationType.Error;
                }
                else if (report.TimeUntilExpiry.HasValue)
                {
                    var remaining = report.TimeUntilExpiry.Value;
                    ExpiryStatus = _localizer.Format("L.Jwt.Expiry.Valid",
                        (int)remaining.TotalHours, remaining.Minutes);
                    ExpiryStatusType = NotificationType.Success;
                }
            }
            else
            {
                ExpiresAt = _localizer.Get("L.Jwt.Expiry.NoExp");
                ExpiryStatus = string.Empty;
                ExpiryStatusType = NotificationType.Info;
            }

            HasResult = true;

            _logger.LogInformation(
                "JWT decoded: alg={Algorithm} typ={TokenType} hasExp={HasExp} expired={IsExpired}",
                report.Algorithm, report.TokenType, report.Expiration.HasValue, report.IsExpired);

            SetStatus(NotificationType.Success, "L.Jwt.Status.Decoded", report.Algorithm, report.TokenType);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("JWT decode cancelled by the user.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT decode failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyPayload()
    {
        if (string.IsNullOrWhiteSpace(DecodedPayload))
        {
            SetStatus(NotificationType.Warning, "L.Jwt.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(DecodedPayload);
            SetStatus(NotificationType.Success, "L.Jwt.Status.PayloadCopied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT payload could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void CopyHeader()
    {
        if (string.IsNullOrWhiteSpace(DecodedHeader))
        {
            SetStatus(NotificationType.Warning, "L.Jwt.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(DecodedHeader);
            SetStatus(NotificationType.Success, "L.Jwt.Status.HeaderCopied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT header could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private async Task SavePayloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DecodedPayload))
        {
            SetStatus(NotificationType.Warning, "L.Jwt.Validation.NothingToSave");
            return;
        }

        IsBusy = true;
        try
        {
            bool saved = await _fileExportService.ExportTextAsync(
                DecodedPayload,
                $"JSON (*.json)|*.json|{_localizer.Get("L.Common.Filter.TextFiles")}|*.txt",
                ".json",
                "jwt-payload");

            SetStatus(saved ? NotificationType.Success : NotificationType.Info,
                saved ? "L.Jwt.Status.Saved" : "L.Jwt.Status.SaveCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT payload could not be saved.");
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

        JwtInput = string.Empty;
        ClearResults();
        HasResult = false;
        SetStatus(NotificationType.Info, "L.Jwt.Status.Cleared");
    }

    private void ClearResults()
    {
        DecodedHeader = string.Empty;
        DecodedPayload = string.Empty;
        Algorithm = "-";
        TokenType = "-";
        ExpiresAt = "-";
        ExpiryStatus = string.Empty;
        ExpiryStatusType = NotificationType.Info;
    }

    private void CancelRunningOperations()
    {
        if (DecodeCancelCommand is not null && DecodeCancelCommand.CanExecute(null))
            DecodeCancelCommand.Execute(null);
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        _logger.LogWarning("JWT {Operation} failed: kind={ErrorKind} code={ErrorCode} detail={Detail}",
            operation, result.ErrorKind, result.ErrorCode, result.TechnicalDetail);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}