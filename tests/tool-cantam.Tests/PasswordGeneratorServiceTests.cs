using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class PasswordGeneratorServiceTests
{
    private readonly PasswordGeneratorService _service = new();

    private static PasswordOptions Opts(
        int len = 16,
        bool upper = true, bool lower = true, bool digits = true, bool symbols = true,
        bool excludeSimilar = false)
        => new(len, upper, lower, digits, symbols, excludeSimilar);

    [Fact]
    public void Generate_Valid_ReturnsPassword()
    {
        var r = _service.Generate(Opts());
        Assert.True(r.IsSuccess);
        Assert.NotNull(r.Value);
        Assert.Equal(16, r.Value!.Password.Length);
    }

    [Fact]
    public void Generate_ExactLength_AllCharsFromPools()
    {
        var r = _service.Generate(Opts(len: 20));
        Assert.True(r.IsSuccess);
        Assert.Equal(20, r.Value!.Password.Length);
    }

    [Fact]
    public void Generate_OnlyUpper_OnlyUppercase()
    {
        var r = _service.Generate(Opts(len: 20, lower: false, digits: false, symbols: false));
        Assert.True(r.IsSuccess);
        Assert.All(r.Value!.Password, c => Assert.True(char.IsUpper(c)));
    }

    [Fact]
    public void Generate_OnlyDigits_OnlyDigits()
    {
        var r = _service.Generate(Opts(len: 20, upper: false, lower: false, symbols: false));
        Assert.True(r.IsSuccess);
        Assert.All(r.Value!.Password, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void Generate_InvalidLength_TooShort_ReturnsValidation()
    {
        var r = _service.Generate(Opts(len: 2));
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Generate_InvalidLength_TooLong_ReturnsValidation()
    {
        var r = _service.Generate(Opts(len: 500));
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Generate_NoCharacterSet_ReturnsValidation()
    {
        var r = _service.Generate(Opts(upper: false, lower: false, digits: false, symbols: false));
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Generate_ExcludeSimilar_NoAmbiguousChars()
    {
        var r = _service.Generate(Opts(len: 50, excludeSimilar: true));
        Assert.True(r.IsSuccess);
        string[] bad = { "I", "l", "1", "O", "0", "o" };
        Assert.DoesNotContain(r.Value!.Password, c => System.Array.IndexOf(bad, c.ToString()) >= 0);
    }

    // ---------- Analyze ----------

    [Fact]
    public void Analyze_Empty_ReturnsZeroScore()
    {
        var r = _service.Analyze("");
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Score);
    }

    [Fact]
    public void Analyze_Null_ReturnsZeroScore()
    {
        var r = _service.Analyze(null);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Score);
    }

    [Fact]
    public void Analyze_Weak_ShortNumeric()
    {
        var r = _service.Analyze("123");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Score < 40);
    }

    [Fact]
    public void Analyze_Strong_LongMixed()
    {
        var r = _service.Analyze("Abc123!@#Def456$%^");
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.Score >= 60);
        Assert.True(r.Value.HasUppercase);
        Assert.True(r.Value.HasLowercase);
        Assert.True(r.Value.HasDigit);
        Assert.True(r.Value.HasSymbol);
    }

    [Fact]
    public void Analyze_TooLong_ReturnsTooLarge()
    {
        var huge = new string('a', 2000);
        var r = _service.Analyze(huge);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
    }
}