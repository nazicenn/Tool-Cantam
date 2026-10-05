// ViewModels/BcryptGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO.Packaging;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class BcryptGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _hashedPassword = string.Empty;

    [ObservableProperty]
    private int _workFactor = 10;

    [ObservableProperty]
    private string _verifyPassword = string.Empty;

    [ObservableProperty]
    private bool _isVerified = false;

    [ObservableProperty]
    private string _verifyResult = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void GenerateHash()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Please enter a password";
            return;
        }

        try
        {
            HashedPassword = BCrypt.Net.BCrypt.HashPassword(Password, WorkFactor);
            StatusText = "✓ Hash generated successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Verify()
    {
        if (string.IsNullOrWhiteSpace(VerifyPassword))
        {
            StatusText = "Please enter password to verify";
            return;
        }

        if (string.IsNullOrWhiteSpace(HashedPassword))
        {
            StatusText = "Please generate a hash first";
            return;
        }

        try
        {
            IsVerified = BCrypt.Net.BCrypt.Verify(VerifyPassword, HashedPassword);
            VerifyResult = IsVerified ? "✓ Password matches!" : "✗ Password does NOT match";
            StatusText = IsVerified ? "✓ Verification successful" : "✗ Verification failed";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GenerateRandomPassword()
    {
        Password = GenerateRandomString(12);
        StatusText = "Random password generated";
    }

    [RelayCommand]
    private void CopyHash()
    {
        if (!string.IsNullOrWhiteSpace(HashedPassword))
        {
            Clipboard.SetText(HashedPassword);
            StatusText = "Hash copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Password = string.Empty;
        HashedPassword = string.Empty;
        VerifyPassword = string.Empty;
        IsVerified = false;
        VerifyResult = string.Empty;
        StatusText = "Cleared";
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[random.Next(chars.Length)];
        return new string(result);
    }
}