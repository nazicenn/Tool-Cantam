using System;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Kriptografik güvenli parola üretim ve güç testi sözleşmesi.
/// Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner; geçersiz parametreler
/// sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IPasswordGeneratorService
{
    int MinLength { get; }
    int MaxLength { get; }

    ToolResult<PasswordReport> Generate(PasswordOptions options);
    ToolResult<PasswordStrengthReport> Analyze(string? password);
}

/// <summary>Parola üretim seçenekleri.</summary>
public sealed record PasswordOptions(
    int Length,
    bool UseUppercase,
    bool UseLowercase,
    bool UseDigits,
    bool UseSymbols,
    bool ExcludeSimilar);

/// <summary>Üretilen parola raporu.</summary>
public sealed record PasswordReport(
    string Password,
    PasswordStrengthReport Strength,
    TimeSpan Elapsed);

/// <summary>Parola güç analizi sonucu.</summary>
public sealed record PasswordStrengthReport(
    int Score,
    string Strength,
    string ColorHex,
    string CrackTime,
    int EntropyBits,
    bool HasUppercase,
    bool HasLowercase,
    bool HasDigit,
    bool HasSymbol,
    bool HasMinLength);