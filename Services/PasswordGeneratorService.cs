using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Kriptografik güvenli parola üretim ve güç analizinin tek uygulaması.
/// <para>
/// Üretim RandomNumberGenerator.GetString kullanır (CSPRNG). Analiz entropy + karakter
/// çeşitliliği + uzunluk üzerinden puanlama yapar. Sızan bilgi yoktur.
/// </para>
/// </summary>
public sealed class PasswordGeneratorService : IPasswordGeneratorService
{
    public const int MinLengthLimit = 4;
    public const int MaxLengthLimit = 256;
    private const int MaxAnalyzeLength = 1024;

    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{};:,.<>?";

    // Benzer görünen karakterler
    private const string SimilarChars = "Il1O0o";

    public int MinLength => MinLengthLimit;
    public int MaxLength => MaxLengthLimit;

    public ToolResult<PasswordReport> Generate(PasswordOptions options)
    {
        if (options is null)
            return ToolResult<PasswordReport>.Fail(
                ToolErrorKind.Validation,
                PasswordGeneratorErrorCodes.InvalidLength,
                "Options are null.");

        if (options.Length < MinLengthLimit || options.Length > MaxLengthLimit)
            return ToolResult<PasswordReport>.Fail(
                ToolErrorKind.Validation,
                PasswordGeneratorErrorCodes.InvalidLength,
                $"Length must be {MinLengthLimit}..{MaxLengthLimit} (got {options.Length}).");

        var pools = new System.Collections.Generic.List<string>();

        if (options.UseUppercase) pools.Add(UppercaseChars);
        if (options.UseLowercase) pools.Add(LowercaseChars);
        if (options.UseDigits) pools.Add(DigitChars);
        if (options.UseSymbols) pools.Add(SymbolChars);

        if (pools.Count == 0)
            return ToolResult<PasswordReport>.Fail(
                ToolErrorKind.Validation,
                PasswordGeneratorErrorCodes.NoCharacterSet,
                "At least one character set must be selected.");

        if (options.ExcludeSimilar)
        {
            pools = pools
                .Select(p => new string(p.Where(c => !SimilarChars.Contains(c)).ToArray()))
                .Where(p => p.Length > 0)
                .ToList();

            if (pools.Count == 0)
                return ToolResult<PasswordReport>.Fail(
                    ToolErrorKind.Validation,
                    PasswordGeneratorErrorCodes.NoCharacterSet,
                    "After excluding similar characters no usable set remains.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Her havuzdan en az bir karakter garanti et
            var chars = new char[options.Length];
            int idx = 0;

            foreach (var pool in pools)
            {
                if (idx >= options.Length) break;
                chars[idx++] = RandomChar(pool);
            }

            // Kalan karakterleri birleşik havuzdan doldur
            string combined = string.Concat(pools);
            while (idx < options.Length)
            {
                chars[idx++] = RandomChar(combined);
            }

            // Karıştır (Fisher-Yates ile CSPRNG)
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            string password = new string(chars);

            stopwatch.Stop();

            var strength = AnalyzeInternal(password);

            return ToolResult<PasswordReport>.Ok(new PasswordReport(
                password,
                strength,
                stopwatch.Elapsed));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResult<PasswordReport>.Fail(
                ToolErrorKind.Unexpected,
                PasswordGeneratorErrorCodes.Unexpected,
                ex.Message);
        }
    }

    public ToolResult<PasswordStrengthReport> Analyze(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return ToolResult<PasswordStrengthReport>.Ok(new PasswordStrengthReport(
                0, "Empty", "#6B7280", "-", 0, false, false, false, false, false));
        }

        if (password.Length > MaxAnalyzeLength)
        {
            return ToolResult<PasswordStrengthReport>.Fail(
                ToolErrorKind.TooLarge,
                PasswordGeneratorErrorCodes.PasswordTooLong,
                $"Password length {password.Length} exceeds {MaxAnalyzeLength}.");
        }

        return ToolResult<PasswordStrengthReport>.Ok(AnalyzeInternal(password));
    }

    private static char RandomChar(string pool) =>
        pool[RandomNumberGenerator.GetInt32(pool.Length)];

    private static PasswordStrengthReport AnalyzeInternal(string password)
    {
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));
        bool hasMinLength = password.Length >= 8;

        int score = 0;
        score += Math.Min(password.Length * 3, 30);

        if (hasUpper) score += 15;
        if (hasLower) score += 15;
        if (hasDigit) score += 15;
        if (hasSymbol) score += 20;

        int variety = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSymbol ? 1 : 0);
        if (variety == 4) score += 5;

        score = Math.Clamp(score, 0, 100);

        int poolSize = 0;
        if (hasLower) poolSize += 26;
        if (hasUpper) poolSize += 26;
        if (hasDigit) poolSize += 10;
        if (hasSymbol) poolSize += 32;

        int entropy = poolSize > 0
            ? (int)(password.Length * Math.Log2(poolSize))
            : 0;

        (string strength, string color) = score switch
        {
            < 20 => ("Very Weak", "#EF4444"),
            < 40 => ("Weak", "#F97316"),
            < 60 => ("Fair", "#EAB308"),
            < 80 => ("Strong", "#22C55E"),
            _ => ("Very Strong", "#16A34A")
        };

        string crackTime = EstimateCrackTime(entropy);

        return new PasswordStrengthReport(
            score, strength, color, crackTime, entropy,
            hasUpper, hasLower, hasDigit, hasSymbol, hasMinLength);
    }

    private static string EstimateCrackTime(int entropyBits)
    {
        if (entropyBits <= 0) return "-";

        double guessesPerSecond = 1e10;
        double combinations = Math.Pow(2, entropyBits);
        double seconds = combinations / guessesPerSecond / 2;

        const double SecondsPerYear = 31536000.0;

        if (seconds < 1) return "Instantly";
        if (seconds < 60) return $"{(int)seconds} sec";
        if (seconds < 3600) return $"{(int)(seconds / 60)} min";
        if (seconds < 86400) return $"{(int)(seconds / 3600)} hours";
        if (seconds < 2592000) return $"{(int)(seconds / 86400)} days";
        if (seconds < SecondsPerYear) return $"{(int)(seconds / 2592000)} months";
        if (seconds < SecondsPerYear * 100) return $"{(int)(seconds / SecondsPerYear)} years";
        if (seconds < SecondsPerYear * 1e6) return $"{(int)(seconds / SecondsPerYear / 1000)}k years";
        return "Centuries";
    }
}