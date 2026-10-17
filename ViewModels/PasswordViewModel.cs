using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Windows.Media;
using tool_cantam.Core.Contracts;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

/// <summary>
/// Password Generator aracının sunum katmanı. İş mantığı <see cref="IPasswordGeneratorService"/> içindedir.
/// Üretim ve güç testi async, iptal edilebilir; PasswordBox yönetimi code-behind'dadır.
/// </summary>
public partial class PasswordViewModel : ObservableObject
{
    private readonly IPasswordGeneratorService _passwordService;
    private readonly IFileExportService _fileExportService;
    private readonly ILocalizerService _localizer;
    private readonly ILogger<PasswordViewModel> _logger;

    // Üretim parametreleri
    [ObservableProperty] private int _passwordLength = 16;
    [ObservableProperty] private bool _useUpper = true;
    [ObservableProperty] private bool _useLower = true;
    [ObservableProperty] private bool _useDigits = true;
    [ObservableProperty] private bool _useSpecial = true;
    [ObservableProperty] private bool _excludeSimilar;

    // Üretilen parola
    [ObservableProperty] private string _generatedPassword = string.Empty;
    [ObservableProperty] private string _generatedStrength = string.Empty;
    [ObservableProperty] private int _generatedScore;
    [ObservableProperty] private SolidColorBrush _generatedColor = new(Colors.Gray);

    // Test parolası
    [ObservableProperty] private string _testPassword = string.Empty;
    [ObservableProperty] private int _strengthPercent;
    [ObservableProperty] private string _strengthText = "Start typing...";
    [ObservableProperty] private SolidColorBrush _strengthColor = new(Colors.Gray);
    [ObservableProperty] private string _crackTime = "-";
    [ObservableProperty] private int _entropyBits;

    // Durum
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private NotificationType _statusType = NotificationType.Info;
    [ObservableProperty] private bool _isBusy;

    public event Action? OnClearRequested;

    public PasswordViewModel(
        IPasswordGeneratorService passwordService,
        IFileExportService fileExportService,
        ILocalizerService localizer,
        ILogger<PasswordViewModel> logger)
    {
        _passwordService = passwordService;
        _fileExportService = fileExportService;
        _localizer = localizer;
        _logger = logger;

        StatusText = _localizer.Get("L.Password.Status.Ready");
    }

    public string LimitsText => _localizer.Format(
        "L.Password.Limits", _passwordService.MinLength, _passwordService.MaxLength);

    partial void OnTestPasswordChanged(string value) => AnalyzePassword(value);

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
        var options = new PasswordOptions(
            PasswordLength,
            UseUpper,
            UseLower,
            UseDigits,
            UseSpecial,
            ExcludeSimilar);

        IsBusy = true;
        try
        {
            ToolResult<PasswordReport> result = await Task.Run(
                () => _passwordService.Generate(options), cancellationToken);

            if (!result.IsSuccess)
            {
                ReportFailure(result, "generate");
                return;
            }

            var report = result.Value!;
            GeneratedPassword = report.Password;
            GeneratedStrength = report.Strength.Strength;
            GeneratedScore = report.Strength.Score;
            GeneratedColor = ParseBrushSafe(report.Strength.ColorHex);

            _logger.LogInformation(
                "Password generated: length={Length} strength={Strength} score={Score}",
                PasswordLength, report.Strength.Strength, report.Strength.Score);

            SetStatus(NotificationType.Success, "L.Password.Status.Generated",
                PasswordLength, report.Strength.Strength);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Password generation cancelled.");
            SetStatus(NotificationType.Warning, "L.Common.Status.Cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password generation failed unexpectedly.");
            SetStatus(NotificationType.Error, "L.Common.Error.Unexpected");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AnalyzePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            StrengthPercent = 0;
            StrengthText = _localizer.Get("L.Password.Strength.StartTyping");
            StrengthColor = new SolidColorBrush(Colors.Gray);
            CrackTime = "-";
            EntropyBits = 0;
            return;
        }

        var result = _passwordService.Analyze(password);
        if (!result.IsSuccess)
        {
            StrengthText = result.ErrorCode ?? "Error";
            StrengthColor = new SolidColorBrush(Colors.Red);
            return;
        }

        var report = result.Value!;
        StrengthPercent = report.Score;
        StrengthText = report.Strength;
        StrengthColor = ParseBrushSafe(report.ColorHex);
        CrackTime = report.CrackTime;
        EntropyBits = report.EntropyBits;
    }

    /// <summary>Boş veya geçersiz renk kodunda gri döner; FormatException fırlatmaz.</summary>
    private static SolidColorBrush ParseBrushSafe(string? colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
            return new SolidColorBrush(Colors.Gray);

        try
        {
            if (new BrushConverter().ConvertFromString(colorHex) is SolidColorBrush brush)
                return brush;
        }
        catch
        {
            // Geçersiz renk: gri dön
        }

        return new SolidColorBrush(Colors.Gray);
    }

    [RelayCommand]
    private void CopyPassword()
    {
        if (string.IsNullOrWhiteSpace(GeneratedPassword))
        {
            SetStatus(NotificationType.Warning, "L.Password.Validation.NothingToCopy");
            return;
        }

        try
        {
            _fileExportService.CopyToClipboard(GeneratedPassword);
            SetStatus(NotificationType.Success, "L.Password.Status.Copied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password could not be copied.");
            SetStatus(NotificationType.Error, "L.Common.Error.Clipboard");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        TestPassword = string.Empty;
        GeneratedPassword = string.Empty;
        GeneratedStrength = string.Empty;
        GeneratedScore = 0;
        OnClearRequested?.Invoke();
        SetStatus(NotificationType.Info, "L.Password.Status.Cleared");
    }

    private void SetStatus(NotificationType type, string messageKey, params object?[] args)
    {
        StatusType = type;
        StatusText = _localizer.Format(messageKey, args);
    }

    private void ReportFailure(IToolResult result, string operation)
    {
        _logger.LogWarning("Password {Operation} failed: kind={ErrorKind} code={ErrorCode}",
            operation, result.ErrorKind, result.ErrorCode);

        SetStatus(NotificationType.Error, result.ErrorCode ?? "L.Common.Error.Unexpected");
    }
}