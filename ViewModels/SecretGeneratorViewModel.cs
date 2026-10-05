// ViewModels/SecretGeneratorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class SecretGeneratorViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 32;

    [ObservableProperty]
    private string _secretType = "API Key";

    [ObservableProperty]
    private string _generatedSecret = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Generate()
    {
        try
        {
            GeneratedSecret = SecretType switch
            {
                "API Key" => GenerateApiKey(Length),
                "JWT Secret" => GenerateJwtSecret(Length),
                "Random Token" => GenerateRandomToken(Length),
                "Password" => GeneratePassword(Length),
                "Hex Key" => GenerateHexKey(Length),
                _ => GenerateApiKey(Length)
            };
            StatusText = $"✓ {SecretType} generated ({GeneratedSecret.Length} chars)";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    private string GenerateApiKey(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return GenerateRandomString(chars, length);
    }

    private string GenerateJwtSecret(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        return GenerateRandomString(chars, length);
    }

    private string GenerateRandomToken(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "").Substring(0, Math.Min(length, 32));
    }

    private string GeneratePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}";
        return GenerateRandomString(chars, length);
    }

    private string GenerateHexKey(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length / 2];
        rng.GetBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }

    private string GenerateRandomString(string chars, int length)
    {
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(result);
    }

    [RelayCommand]
    private void CopySecret()
    {
        if (!string.IsNullOrWhiteSpace(GeneratedSecret))
        {
            Clipboard.SetText(GeneratedSecret);
            StatusText = "Secret copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Clear()
    {
        GeneratedSecret = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void LoadSample()
    {
        SecretType = "API Key";
        Length = 32;
        Generate();
    }
}