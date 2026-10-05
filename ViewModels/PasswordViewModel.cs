using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PasswordViewModel : ObservableObject
{
    private readonly IPasswordService _passwordService;
    private readonly IFileExportService _fileExportService;

    // Clear işlemi sonrası çağrılacak callback
    public Action? OnClearRequested { get; set; }

    [ObservableProperty] private int _passwordLength = 16;
    [ObservableProperty] private bool _useUpper = true;
    [ObservableProperty] private bool _useLower = true;
    [ObservableProperty] private bool _useDigits = true;
    [ObservableProperty] private bool _useSpecial = true;
    [ObservableProperty] private string _generatedPassword = string.Empty;

    [ObservableProperty] private string _testPassword = string.Empty;
    [ObservableProperty] private string _strengthText = "Enter a password to test";
    [ObservableProperty] private int _strengthPercent = 0;
    [ObservableProperty] private string _strengthColor = "#444444";
    [ObservableProperty] private string _statusText = "Ready.";

    public PasswordViewModel(IPasswordService passwordService, IFileExportService fileExportService)
    {
        _passwordService = passwordService;
        _fileExportService = fileExportService;
        Generate();
    }

    [RelayCommand]
    private void Generate()
    {
        try
        {
            GeneratedPassword = _passwordService.Generate(PasswordLength, UseUpper, UseLower, UseDigits, UseSpecial);
            CheckStrength(GeneratedPassword);
            StatusText = "Password generated successfully.";
        }
        catch (Exception ex)
        {
            GeneratedPassword = $"Error: {ex.Message}";
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        PasswordLength = 16;
        UseUpper = true;
        UseLower = true;
        UseDigits = true;
        UseSpecial = true;
        GeneratedPassword = string.Empty;
        TestPassword = string.Empty;
        StrengthText = "Enter a password to test";
        StrengthPercent = 0;
        StrengthColor = "#444444";
        StatusText = "Cleared. Ready.";

        // PasswordBox'u temizlemek için callback'i tetikle
        OnClearRequested?.Invoke();
    }

    [RelayCommand]
    private void CopyPassword()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedPassword))
        {
            _fileExportService.CopyToClipboard(GeneratedPassword);
            StatusText = "Password copied to clipboard.";
        }
    }

    partial void OnTestPasswordChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            CheckStrength(value);
        else
        {
            StrengthText = "Enter a password to test";
            StrengthPercent = 0;
            StrengthColor = "#444444";
            StatusText = "Ready.";
        }
    }

    private void CheckStrength(string password)
    {
        int score = 0;
        int maxScore = 7;

        if (password.Length >= 8) score++;
        if (password.Length >= 12) score++;
        if (password.Length >= 16) score++;

        if (Regex.IsMatch(password, "[a-z]")) score++;
        if (Regex.IsMatch(password, "[A-Z]")) score++;
        if (Regex.IsMatch(password, "[0-9]")) score++;
        if (Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++;

        int percent = (int)((double)score / maxScore * 100);
        StrengthPercent = percent;

        if (percent >= 80)
        {
            StrengthText = "Very Strong";
            StrengthColor = "#00FF41";
        }
        else if (percent >= 60)
        {
            StrengthText = "Strong";
            StrengthColor = "#88FF00";
        }
        else if (percent >= 40)
        {
            StrengthText = "Medium";
            StrengthColor = "#FFA500";
        }
        else if (percent >= 20)
        {
            StrengthText = "Weak";
            StrengthColor = "#FF4444";
        }
        else
        {
            StrengthText = "Very Weak";
            StrengthColor = "#AA0000";
        }
    }
}