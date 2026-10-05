using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.ViewModels;

public partial class PwnedViewModel : ObservableObject
{
    private readonly IPwnedService _pwnedService;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private string _resultText = string.Empty;

    [ObservableProperty]
    private string _resultColor = "#888888";

    [ObservableProperty]
    private string _suggestedPassword = string.Empty;

    [ObservableProperty]
    private bool _showSuggestion;

    public PwnedViewModel(IPwnedService pwnedService)
    {
        _pwnedService = pwnedService;
    }

    private string GenerateSuggestion(string originalPassword)
    {
        if (string.IsNullOrWhiteSpace(originalPassword))
            return "MySecurePass_123";

        var random = new Random();
        var numbers = random.Next(10, 999).ToString();
        var specials = new[] { "_", "@", "#" };
        var special = specials[random.Next(specials.Length)];

        var suggestion = originalPassword.Trim();

        if (suggestion.Length < 8)
            suggestion = suggestion + "Secure";

        if (!System.Text.RegularExpressions.Regex.IsMatch(suggestion, @"[0-9]"))
            suggestion = suggestion + numbers;

        if (!System.Text.RegularExpressions.Regex.IsMatch(suggestion, @"[\W_]"))
            suggestion = suggestion + special;

        return suggestion;
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Please enter a password";
            return;
        }

        IsChecking = true;
        ShowSuggestion = false;
        SuggestedPassword = string.Empty;
        ResultText = string.Empty;
        ResultColor = "#888888";
        StatusText = "Checking with HIBP API...";

        try
        {
            // Servis mevcut k-anonim yaklaşımını kullanır: parola değil, SHA-1 öneki gönderilir.
            int count = await _pwnedService.CheckPasswordAsync(Password);

            if (count < 0)
            {
                StatusText = "✗ Error: Could not reach HIBP API";
                ResultText = string.Empty;
                ResultColor = "#FF4444";
            }
            else if (count == 0)
            {
                StatusText = "✓ Password not found in any known breach";
                ResultText = "SAFE - not found in any known breach";
                ResultColor = "#00FF41";
            }
            else
            {
                StatusText = $"✗ Password found {count:N0} time(s) in known breaches";
                ResultText = $"PWNED - seen {count:N0} time(s) in known breaches";
                ResultColor = "#FF4444";
                SuggestedPassword = GenerateSuggestion(Password);
                ShowSuggestion = true;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            ResultText = string.Empty;
            ResultColor = "#FF4444";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void UseSuggestion()
    {
        if (!string.IsNullOrWhiteSpace(SuggestedPassword))
        {
            Password = SuggestedPassword;
            SuggestedPassword = string.Empty;
            ShowSuggestion = false;
            ResultText = string.Empty;
            StatusText = "Suggestion applied. Click CHECK to test it.";
            ResultColor = "#888888";
        }
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (!string.IsNullOrWhiteSpace(ResultText))
        {
            System.Windows.Clipboard.SetText(ResultText);
            StatusText = "Copied to clipboard!";
        }
        else
        {
            StatusText = "Nothing to copy";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Password = string.Empty;
        ResultText = string.Empty;
        SuggestedPassword = string.Empty;
        ShowSuggestion = false;
        StatusText = "Ready.";
        ResultColor = "#888888";
    }
}