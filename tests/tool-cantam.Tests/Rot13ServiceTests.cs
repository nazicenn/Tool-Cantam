using tool_cantam.Services.ErrorCodes;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Harf döndürme servisinin sözleşmesini kilitler: klasik ROT13 vektörleri, kaydırmanın
/// normalleştirilmesi, yalnızca ASCII harflerin dönmesi ve Unicode güvenliği.
/// </summary>
public class Rot13ServiceTests
{
    private readonly Rot13Service _service = new();

    // ---------------------------------------------------------------------
    // Klasik ROT13
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Hello, World!", "Uryyb, Jbeyq!")]
    [InlineData("abc", "nop")]
    [InlineData("ABC", "NOP")]
    [InlineData("nop", "abc")]
    [InlineData("Why did the chicken cross the road?", "Jul qvq gur puvpxra pebff gur ebnq?")]
    [InlineData("AttacK at dawn", "NggnpX ng qnja")]
    public void Rotate_thirteen_matches_known_vectors(string input, string expected)
    {
        ToolResult<string> result = _service.Rotate(input, 13);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Rotate_thirteen_is_its_own_inverse()
    {
        const string original = "The quick brown fox jumps over the lazy dog 123";

        string once = _service.Rotate(original, 13).Value!;
        string twice = _service.Rotate(once, 13).Value!;

        Assert.NotEqual(original, once);
        Assert.Equal(original, twice);
    }

    [Fact]
    public void Rotate_preserves_letter_case()
    {
        ToolResult<string> result = _service.Rotate("AbCdEf", 13);

        Assert.Equal("NoPqRs", result.Value);
    }

    // ---------------------------------------------------------------------
    // Harf olmayan karakterler
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("1234567890")]
    [InlineData("!@#$%^&*()_+-=[]{};':\",./<>?")]
    [InlineData("ğüşıöçĞÜŞİÖÇ")]
    [InlineData("àéîõü")]
    [InlineData("😀🎉")]
    [InlineData("\t\n\r ")]
    public void Rotate_leaves_non_ascii_letters_unchanged(string input)
    {
        ToolResult<string> result = _service.Rotate(input, 13);

        Assert.True(result.IsSuccess);
        Assert.Equal(input, result.Value);
    }

    [Fact]
    public void Rotate_mixes_letters_and_symbols_correctly()
    {
        ToolResult<string> result = _service.Rotate("Türkçe: 42 + Hello 😀", 13);

        Assert.Equal("Güexçr: 42 + Uryyb 😀", result.Value);
    }

    [Fact]
    public void Rotate_never_changes_the_length_and_keeps_surrogate_pairs_intact()
    {
        const string input = "a😀b🎉c";

        ToolResult<string> result = _service.Rotate(input, 13);

        Assert.Equal(input.Length, result.Value!.Length);
        Assert.Equal("n😀o🎉p", result.Value);
    }

    // ---------------------------------------------------------------------
    // Kaydırma değeri
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(1, "abc", "bcd")]
    [InlineData(25, "abc", "zab")]
    [InlineData(-1, "abc", "zab")]
    [InlineData(-13, "abc", "nop")]
    [InlineData(52, "abc", "abc")]
    [InlineData(0, "abc", "abc")]
    [InlineData(26, "abc", "abc")]
    public void Rotate_normalizes_the_shift(int shift, string input, string expected)
    {
        ToolResult<string> result = _service.Rotate(input, shift);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_and_decode_are_inverses_for_any_shift()
    {
        const string original = "Gizli olmayan metin / not a secret 123";

        for (int shift = 1; shift < 26; shift++)
        {
            string encoded = _service.Rotate(original, shift).Value!;
            string decoded = _service.Rotate(encoded, -shift).Value!;

            Assert.Equal(original, decoded);
        }
    }

    // ---------------------------------------------------------------------
    // Sınırlar ve hatalar
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Rotate_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Rotate(input, 13);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Rotate_reports_unpaired_surrogates()
    {
        ToolResult<string> result = _service.Rotate("\uD800", 13);

        Assert.False(result.IsSuccess);
        Assert.Equal(Rot13ErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Rotate_reports_too_large_input()
    {
        ToolResult<string> result = _service.Rotate(new string('a', Rot13Service.MaxTextLengthLimit + 1), 13);

        Assert.False(result.IsSuccess);
        Assert.Equal(Rot13ErrorCodes.InputTooLarge, result.ErrorCode);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
    }
}
