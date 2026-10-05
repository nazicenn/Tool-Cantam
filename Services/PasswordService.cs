using System;
using System.Security.Cryptography;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Kriptografik güvenli parola üretim servis uygulaması.
/// RandomNumberGenerator.GetString kullanarak CSPRNG tabanlı üretim yapar.
/// </summary>
public class PasswordService : IPasswordService
{
    public string Generate(int length, bool upper, bool lower, bool digits, bool special)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Password length must be 1 or greater.");

        // Minimum ayırma ile karakter havuzu birleşimi
        string pool = string.Concat(
            upper   ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "",
            lower   ? "abcdefghijklmnopqrstuvwxyz" : "",
            digits  ? "0123456789"                 : "",
            special ? "!@#$%^&*()_-+=<>?"          : ""
        );

        if (string.IsNullOrEmpty(pool))
            throw new ArgumentException("At least one character set must be selected (uppercase, lowercase, digits, or symbols).");

        // .NET 8/10 yerleşik CSPRNG string üretim metodu
        return RandomNumberGenerator.GetString(pool, length);
    }
}
