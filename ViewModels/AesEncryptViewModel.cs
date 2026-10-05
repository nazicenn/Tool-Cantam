// ViewModels/AesEncryptViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace tool_cantam.ViewModels;

public partial class AesEncryptViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void Encrypt()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to encrypt";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Please enter a password";
            return;
        }

        try
        {
            var encrypted = EncryptString(InputText, Password);
            OutputText = Convert.ToBase64String(encrypted);
            StatusText = "✓ Encrypted successfully";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Decrypt()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusText = "Please enter text to decrypt";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Please enter the password";
            return;
        }

        try
        {
            var encrypted = Convert.FromBase64String(InputText);

            // Minimum length check: salt(16) + iv(16) = 32
            if (encrypted.Length < 32)
            {
                StatusText = "✗ Invalid encrypted data";
                return;
            }

            var decrypted = DecryptString(encrypted, Password);
            OutputText = decrypted;
            StatusText = "✓ Decrypted successfully";
        }
        catch (FormatException)
        {
            StatusText = "✗ Invalid Base64 format";
        }
        catch (Exception ex)
        {
            StatusText = $"✗ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GeneratePassword()
    {
        Password = GenerateRandomPassword(16);
        StatusText = "✓ Random password generated (16 chars)";
    }

    [RelayCommand]
    private void Clear()
    {
        InputText = string.Empty;
        OutputText = string.Empty;
        Password = string.Empty;
        StatusText = "Cleared";
    }

    [RelayCommand]
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            Clipboard.SetText(OutputText);
            StatusText = "Copied to clipboard!";
        }
    }

    [RelayCommand]
    private void Swap()
    {
        (InputText, OutputText) = (OutputText, InputText);
        StatusText = "Swapped";
    }

    private byte[] EncryptString(string plainText, string password)
    {
        using var aes = Aes.Create();

        // Production-grade settings
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Generate random salt
        var salt = RandomNumberGenerator.GetBytes(16);

        // Generate random IV (independent from password)
        aes.GenerateIV();

        // Derive key from password and salt
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);

        aes.Key = key;
        aes.IV = aes.IV; // Use generated IV

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var ms = new MemoryStream();
        // Write salt + IV + ciphertext
        ms.Write(salt, 0, salt.Length);
        ms.Write(aes.IV, 0, aes.IV.Length);

        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(plainBytes, 0, plainBytes.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }

    private string DecryptString(byte[] cipherText, string password)
    {
        using var aes = Aes.Create();

        // Production-grade settings
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        // Extract salt (first 16 bytes)
        var salt = new byte[16];
        Array.Copy(cipherText, 0, salt, 0, 16);

        // Extract IV (next 16 bytes)
        var iv = new byte[16];
        Array.Copy(cipherText, 16, iv, 0, 16);

        // Extract encrypted data (remaining bytes)
        var encryptedData = new byte[cipherText.Length - 32];
        Array.Copy(cipherText, 32, encryptedData, 0, encryptedData.Length);

        // Derive key from password and salt
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);

        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(encryptedData);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    private string GenerateRandomPassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";

        // Cryptographic random - not predictable!
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(result);
    }
}