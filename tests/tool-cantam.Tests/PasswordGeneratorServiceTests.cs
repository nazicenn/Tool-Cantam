using System;
using System.Linq;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Parola üretim ve güç analizi servisinin sözleşmesini kilitler: uzunluk, karakter setleri,
/// benzer karakter hariç tutma, güç puanlama, entropi ve hata yolları.
/// </summary>
public class PasswordGeneratorServiceTests
{
    private readonly PasswordGeneratorService _service = new();

    private static PasswordOptions Options(
        int length = 16,
        bool upper = true,
        bool lower = true,
        bool digits = true,
        bool symbols = true,
        bool excludeSimilar = false) =>
        new(length, upper, lower, digits, symbols, excludeSimilar);

    // ---------------------------------------------------------------------
    // Üretim
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void Generate_produces_password_of_requested_length(int length)
    {
        var r = _service.Generate(Options(length: length));
        Assert.True(r.IsSuccess);
        Assert.Equal(length, r.Value!.Password.Length);
    }

    [Fact]
    public void Generate_with_all_sets_includes_each_kind()
    {
        var r = _service.Generate(Options(length: 32));
        Assert.True(r.IsSuccess);

        var pwd = r.Value!.Password;
        Assert.Contains(pwd, char.IsUpper);
        Assert.Contains(pwd, char.IsLower);
        Assert.Contains(pwd, char.IsDigit);
        Assert.Contains(pwd, c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public void Generate_only_uppercase_contains_no_lowercase_or_digits()
    {
        var r = _service.Generate(Options(length: 20, upper: true, lower: false, digits: false, symbols: false));
        Assert.True(r.IsSuccess);

        var pwd = r.Value!.Password;
        Assert.All(pwd, c => Assert.True(char.IsUpper(c)));
    }

    [Fact]
    public void Generate_excludes_similar_characters_when_requested()
    {
        var r = _service.Generate(Options(length: 32, excludeSimilar: true));
        Assert.True(r.IsSuccess);

        var pwd = r.Value!.Password;
        Assert.DoesNotContain('I', pwd);
        Assert.DoesNotContain('l', pwd);
        Assert.DoesNotContain('1', pwd);
        Assert.DoesNotContain('O', pwd);
        Assert.DoesNotContain('0', pwd);
        Assert.DoesNotContain('o', pwd);
    }

    [Fact]
    public void Generate_returns_a_strong_analysis_for_a_typical_password()
    {
        var r = _service.Generate(Options(length: 24));
        Assert.True(r.IsSuccess);

        var report = r.Value!;
        Assert.True(report.Strength.Score > 60);
        Assert.False(string.IsNullOrEmpty(report.Strength.Strength));
        Assert.False(string.IsNullOrEmpty(report.Strength.ColorHex));
        Assert.False(string.IsNullOrEmpty(report.Strength.CrackTime));
    }

    [Fact]
    public void Generate_is_deterministic_across_calls_when_only_requested_sets_are_used()
    {
        // Farklı iki üretim aynı olmamalı (CSPRNG)
        var a = _service.Generate(Options(length: 32)).Value!.Password;
        var b = _service.Generate(Options(length: 32)).Value!.Password;
        Assert.NotEqual(a, b);
    }

    // ---------------------------------------------------------------------
    // Hata yolları
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    public void Generate_rejects_invalid_length(int length)
    {
        var r = _service.Generate(Options(length: length));
        Assert.False(r.IsSuccess);
        Assert.Equal(PasswordGeneratorErrorCodes.InvalidLength, r.ErrorCode);
    }

    [Fact]
    public void Generate_rejects_when_no_character_set_selected()
    {
        var r = _service.Generate(Options(upper: false, lower: false, digits: false, symbols: false));
        Assert.False(r.IsSuccess);
        Assert.Equal(PasswordGeneratorErrorCodes.NoCharacterSet, r.ErrorCode);
    }

    [Fact]
    public void Generate_rejects_when_all_sets_become_empty_after_excluding_similar()
    {
        // Sadece "Il1O0o" içeren setler — benzerleri çıkarılınca boş kalır
        var r = _service.Generate(new PasswordOptions(16, true, false, false, false, true));
        // "ABC..." gibi diğer harfler hâlâ var, bu yüzden başarılı olmalı
        Assert.True(r.IsSuccess);
    }

    // ---------------------------------------------------------------------
    // Analiz
    // ---------------------------------------------------------------------

    [Fact]
    public void Analyze_very_short_password_is_weak()
    {
        var r = _service.Analyze("abc");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Score < 40);
        Assert.False(r.Value.HasMinLength);
    }

    [Fact]
    public void Analyze_strong_password_scores_high()
    {
        var r = _service.Analyze("Tr0ub4dor&3");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Score >= 60);
        Assert.True(r.Value.HasUppercase);
        Assert.True(r.Value.HasLowercase);
        Assert.True(r.Value.HasDigit);
        Assert.True(r.Value.HasSymbol);
    }

    [Fact]
    public void Analyze_long_complex_password_reaches_very_strong()
    {
        var r = _service.Analyze("Correct-Horse-Battery-Staple-2024!");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Score >= 80);
        Assert.Equal("Very Strong", r.Value.Strength);
    }

    [Fact]
    public void Analyze_digits_only_has_low_entropy()
    {
        var r = _service.Analyze("12345678");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.HasDigit);
        Assert.False(r.Value.HasUppercase);
        Assert.False(r.Value.HasLowercase);
        Assert.False(r.Value.HasSymbol);
    }

    [Fact]
    public void Analyze_entropy_grows_with_length_and_variety()
    {
        var weak = _service.Analyze("aaaa").Value!.EntropyBits;
        var medium = _service.Analyze("aA1!").Value!.EntropyBits;
        var strong = _service.Analyze("aA1!aA1!aA1!aA1!").Value!.EntropyBits;

        Assert.True(weak < medium);
        Assert.True(medium < strong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Analyze_empty_returns_zero(string? input)
    {
        var r = _service.Analyze(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Score);
        Assert.Equal(0, r.Value.EntropyBits);
    }

    [Fact]
    public void Analyze_too_long_is_reported()
    {
        var r = _service.Analyze(new string('a', 2000));
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
        Assert.Equal(PasswordGeneratorErrorCodes.PasswordTooLong, r.ErrorCode);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.MinLength > 0);
        Assert.True(_service.MaxLength >= 128);
    }
}