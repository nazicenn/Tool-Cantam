// ViewModels/UserAgentParserViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class UserAgentParserViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36";

    [ObservableProperty]
    private string _browser = string.Empty;

    [ObservableProperty]
    private string _operatingSystem = string.Empty;

    [ObservableProperty]
    private string _device = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    // PUBLIC yapıldı - Code-behind'den erişmek için
    public void Parse()
    {
        if (string.IsNullOrWhiteSpace(UserAgent))
        {
            StatusText = "Please enter a User Agent string";
            return;
        }

        try
        {
            var ua = UserAgent;

            // Browser detection
            if (ua.Contains("Edg/"))
            {
                var match = Regex.Match(ua, @"Edg/(\d+\.?\d*)");
                Browser = $"Microsoft Edge {match.Groups[1].Value}";
            }
            else if (ua.Contains("Firefox/"))
            {
                var match = Regex.Match(ua, @"Firefox/(\d+\.?\d*)");
                Browser = $"Firefox {match.Groups[1].Value}";
            }
            else if (ua.Contains("Chrome/") && !ua.Contains("Edg/"))
            {
                var match = Regex.Match(ua, @"Chrome/(\d+\.?\d*)");
                Browser = $"Chrome {match.Groups[1].Value}";
            }
            else if (ua.Contains("Safari/") && !ua.Contains("Chrome/"))
            {
                var match = Regex.Match(ua, @"Version/(\d+\.?\d*)");
                Browser = $"Safari {match.Groups[1].Value}";
            }
            else if (ua.Contains("OPR/") || ua.Contains("Opera/"))
            {
                var match = Regex.Match(ua, @"(?:OPR|Opera)/(\d+\.?\d*)");
                Browser = $"Opera {match.Groups[1].Value}";
            }
            else
            {
                Browser = "Unknown Browser";
            }

            // OS detection
            if (ua.Contains("Windows NT 10.0"))
                OperatingSystem = "Windows 10/11";
            else if (ua.Contains("Windows NT 6.1"))
                OperatingSystem = "Windows 7";
            else if (ua.Contains("Windows NT 6.2"))
                OperatingSystem = "Windows 8";
            else if (ua.Contains("Windows NT 6.3"))
                OperatingSystem = "Windows 8.1";
            else if (ua.Contains("Mac OS X"))
            {
                var match = Regex.Match(ua, @"Mac OS X (\d+)[_\.](\d+)");
                if (match.Success)
                    OperatingSystem = $"macOS {match.Groups[1]}.{match.Groups[2]}";
                else
                    OperatingSystem = "macOS";
            }
            else if (ua.Contains("iPhone"))
                OperatingSystem = "iOS";
            else if (ua.Contains("iPad"))
                OperatingSystem = "iPadOS";
            else if (ua.Contains("Android"))
                OperatingSystem = "Android";
            else if (ua.Contains("Linux"))
                OperatingSystem = "Linux";
            else
                OperatingSystem = "Unknown OS";

            // Device detection
            if (ua.Contains("iPhone"))
                Device = "iPhone";
            else if (ua.Contains("iPad"))
                Device = "iPad";
            else if (ua.Contains("Android") && ua.Contains("Mobile"))
                Device = "Android Phone";
            else if (ua.Contains("Android"))
                Device = "Android Tablet";
            else if (ua.Contains("Windows NT") && (ua.Contains("Phone") || ua.Contains("Mobile")))
                Device = "Windows Phone";
            else if (ua.Contains("Windows NT"))
                Device = "Desktop PC";
            else if (ua.Contains("Mac OS X"))
                Device = "Mac";
            else if (ua.Contains("Linux"))
                Device = "Linux PC";
            else
                Device = "Unknown Device";

            StatusText = "✓ User Agent parsed successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
            ClearResults();
        }
    }

    [RelayCommand]
    private void ParseCommand()
    {
        Parse();
    }

    [RelayCommand]
    private void Clear()
    {
        UserAgent = string.Empty;
        ClearResults();
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyUserAgent()
    {
        if (!string.IsNullOrWhiteSpace(UserAgent))
        {
            Clipboard.SetText(UserAgent);
            StatusText = "User Agent copied to clipboard!";
        }
    }

    private void ClearResults()
    {
        Browser = string.Empty;
        OperatingSystem = string.Empty;
        Device = string.Empty;
    }
}