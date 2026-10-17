using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Metin ters çevirme servisinin sözleşmesini kilitler: her mod için bilinen giriş/çıkış,
/// Unicode davranışı, boyut sınırı ve geçersiz giriş yolları.
/// </summary>
public class StringReverserServiceTests
{
    private readonly StringReverserService _service = new();

    // ---------------------------------------------------------------------
    // ReverseAll
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("abc", "cba")]
    [InlineData("hello", "olleh")]
    [InlineData("a", "a")]
    [InlineData("ab", "ba")]
    public void ReverseAll_reverses_every_character(string input, string expected)
    {
        var r = _service.Reverse(input, StringReverseMode.ReverseAll);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Fact]
    public void ReverseAll_preserves_emoji_surrogate_pairs()
    {
        var r = _service.Reverse("a😀b", StringReverseMode.ReverseAll);
        Assert.True(r.IsSuccess);
        Assert.Equal("b😀a", r.Value);
    }

    [Fact]
    public void ReverseAll_preserves_turkish_characters()
    {
        var r = _service.Reverse("ğüşiöç", StringReverseMode.ReverseAll);
        Assert.True(r.IsSuccess);
        Assert.Equal("çöişüğ", r.Value);
    }

    // ---------------------------------------------------------------------
    // ReverseWords
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("hello world", "world hello")]
    [InlineData("a b c", "c b a")]
    [InlineData("one", "one")]
    public void ReverseWords_reverses_word_order(string input, string expected)
    {
        var r = _service.Reverse(input, StringReverseMode.ReverseWords);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Fact]
    public void ReverseWords_keeps_whitespace_layout()
    {
        var r = _service.Reverse("a  b   c", StringReverseMode.ReverseWords);
        Assert.True(r.IsSuccess);
        Assert.Equal("c   b  a", r.Value);
    }

    // ---------------------------------------------------------------------
    // ReverseEachWord
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("abc def", "cba fed")]
    [InlineData("hello world", "olleh dlrow")]
    [InlineData("one", "eno")]
    public void ReverseEachWord_reverses_letters_in_place(string input, string expected)
    {
        var r = _service.Reverse(input, StringReverseMode.ReverseEachWord);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Fact]
    public void ReverseEachWord_keeps_spaces_and_newlines()
    {
        var r = _service.Reverse("ab\ncd ef", StringReverseMode.ReverseEachWord);
        Assert.True(r.IsSuccess);
        Assert.Equal("ba\ndc fe", r.Value);
    }

    // ---------------------------------------------------------------------
    // ReverseLines
    // ---------------------------------------------------------------------

    [Fact]
    public void ReverseLines_reverses_line_order()
    {
        var r = _service.Reverse("l1\nl2\nl3", StringReverseMode.ReverseLines);
        Assert.True(r.IsSuccess);
        Assert.Equal("l3\nl2\nl1", r.Value);
    }

    [Fact]
    public void ReverseLines_keeps_content_of_each_line()
    {
        var r = _service.Reverse("abc\ndef", StringReverseMode.ReverseLines);
        Assert.True(r.IsSuccess);
        Assert.Equal("def\nabc", r.Value);
    }

    [Fact]
    public void ReverseLines_handles_single_line()
    {
        var r = _service.Reverse("only", StringReverseMode.ReverseLines);
        Assert.True(r.IsSuccess);
        Assert.Equal("only", r.Value);
    }

    // ---------------------------------------------------------------------
    // ReverseLinesAndContent
    // ---------------------------------------------------------------------

    [Fact]
    public void ReverseLinesAndContent_reverses_both_order_and_content()
    {
        var r = _service.Reverse("ab\ncd", StringReverseMode.ReverseLinesAndContent);
        Assert.True(r.IsSuccess);
        Assert.Equal("dc\nba", r.Value);
    }

    [Fact]
    public void ReverseLinesAndContent_preserves_crlf()
    {
        var r = _service.Reverse("ab\r\ncd\r\n", StringReverseMode.ReverseLinesAndContent);
        Assert.True(r.IsSuccess);
        Assert.Contains("\r\n", r.Value);
    }

    // ---------------------------------------------------------------------
    // Edge cases
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_input_returns_empty(string? input)
    {
        var r = _service.Reverse(input, StringReverseMode.ReverseAll);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Unpaired_surrogate_is_reported()
    {
        var r = _service.Reverse("test\uD800", StringReverseMode.ReverseAll);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
        Assert.Equal(StringReverserErrorCodes.InvalidUnicode, r.ErrorCode);
    }

    [Fact]
    public void Too_large_input_is_reported()
    {
        string huge = new string('a', StringReverserService.MaxTextLengthLimit + 1);
        var r = _service.Reverse(huge, StringReverseMode.ReverseAll);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
        Assert.Equal(StringReverserErrorCodes.InputTooLarge, r.ErrorCode);
    }

    [Fact]
    public void Supported_modes_contains_all_five_modes()
    {
        Assert.Equal(5, _service.SupportedModes.Count);
        Assert.Contains(StringReverseMode.ReverseAll, _service.SupportedModes);
        Assert.Contains(StringReverseMode.ReverseLinesAndContent, _service.SupportedModes);
    }
}