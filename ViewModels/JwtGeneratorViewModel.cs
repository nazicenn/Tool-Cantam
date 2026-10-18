using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class JwtGeneratorViewModel : ObservableObject
{
    private readonly IJwtGeneratorService _service;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<JwtGeneratorViewModel> _logger;

    [ObservableProperty] private string _secretKey = string.Empty;
    [ObservableProperty] private string _issuer = "myApp";
    [ObservableProperty] private string _audience = "myUsers";
    [ObservableProperty] private int _expiryMinutes = 60;
    [ObservableProperty] private string _claims = "userId:123\nusername:admin\nrole:Admin";
    [ObservableProperty] private JwtAlgorithm _algorithm = JwtAlgorithm.HS256;
    [ObservableProperty] private string _generatedToken = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public JwtGeneratorViewModel(
        IJwtGeneratorService service,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<JwtGeneratorViewModel> logger)
    {
        _service = service;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;
        SecretKey = GenerateRandomKey();
        StatusText = _localizer.Get("L.JwtGenerator.Status.Ready");
    }

    public string LimitsText => _localizer.Format("L.JwtGenerator.Limits", _service.MinKeyLength, _service.MaxKeyLength);
    public JwtAlgorithm[] Algorithms => new[] { JwtAlgorithm.HS256, JwtAlgorithm.HS384, JwtAlgorithm.HS512 };

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync(CancellationToken ct)
    {
        IsBusy = true;
        try
        {
            var opts = new JwtOptions(SecretKey, Issuer, Audience, ExpiryMinutes, Claims, Algorithm);
            var r = await _service.GenerateAsync(opts, ct);
            if (!r.IsSuccess)
            {
                ReportFailure(r, "generate");
                GeneratedToken = string.Empty;
                return;
            }
            GeneratedToken = r.Value!.Token;
            _logger.LogInformation("JWT generated: {Alg}, expires={Exp}, claims={N}",
                r.Value.Algorithm, r.Value.ExpiresAt, r.Value.ClaimCount);
            SetStatus(NotificationType.Success, "L.JwtGenerator.Status.Generated", r.Value.Algorithm, r.Value.ClaimCount);
        }
        catch (OperationCanceledException) { SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled"); }
        catch (Exception ex) { _logger.LogError(ex, "JWT generate failed."); SetStatus(NotificationType.Error, "L.Common.Error.Unexpected"); }
        finally { IsBusy = false; }
    }

    private bool CanGenerate() => !string.IsNullOrWhiteSpace(SecretKey);

    [RelayCommand]
    private void GenerateStrongKey() => SecretKey = GenerateRandomKey();

    private static string GenerateRandomKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [RelayCommand]
    private void CopyToken()
    {
        if (string.IsNullOrWhiteSpace(GeneratedToken))
        {
            SetStatus(NotificationType.Warning, "L.JwtGenerator.Validation.NothingToCopy");
            return;
        }
        try
        {
            _fileExportService.CopyToClipboard(GeneratedToken);
            SetStatus(NotificationType.Success, "L.JwtGenerator.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void SaveToken()
    {
        if (string.IsNullOrWhiteSpace(GeneratedToken))
        {
            SetStatus(NotificationType.Warning, "L.JwtGenerator.Validation.NothingToSave");
            return;
        }
        var path = _fileExportService.RequestSavePath(_localizer.Get("L.JwtGenerator.Filter.Text"), ".jwt", "token");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.WriteAllText(path, GeneratedToken);
            SetStatus(NotificationType.Success, "L.JwtGenerator.Status.Saved", Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            SetStatus(NotificationType.Error, "L.Common.Error.FileSave");
        }
    }

    [RelayCommand]
    private void LoadSample()
    {
        SecretKey = GenerateRandomKey();
        Issuer = "myApp";
        Audience = "myUsers";
        ExpiryMinutes = 60;
        Claims = "userId:123\nusername:admin\nrole:Admin";
        SetStatus(NotificationType.Info, "L.JwtGenerator.Status.SampleLoaded");
    }

    [RelayCommand]
    private void Clear()
    {
        GeneratedToken = string.Empty;
        SetStatus(NotificationType.Info, "L.JwtGenerator.Status.Cleared");
    }

    private void SetStatus(NotificationType t, string k, params object?[] a)
    {
        StatusType = t;
        StatusText = _localizer.Format(k, a);
    }

    private void ReportFailure<T>(ToolResult<T> r, string op)
    {
        _logger.LogWarning("JwtGenerator {Op} failed: {Kind} {Code}", op, r.ErrorKind, r.ErrorCode);
        SetStatus(NotificationType.Error, r.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}