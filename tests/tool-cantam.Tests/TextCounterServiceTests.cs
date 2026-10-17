using System;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Metin sayacı servisinin sözleşmesini kilitler: karakter/kelime/cümle/paragraf sayımı,
/// okuma/konuşma süreleri, en çok kullanılan kelimeler ve hata yolları.
/// </summary>
public class TextCounterServiceTests
{
    private readonly TextCounterService _service = new();

    [Fact]
    public void Count_character_total_includes_whitespace()
    {
        var r = _service.Count("hello world");
        Assert.True(r.IsSuccess);
        Assert.Equal(11, r.Value!.CharacterCount);
    }

    [Fact]
    public void Count_no_spaces_excludes_all_whitespace()
    {
        var r = _service.Count("hello world\nfoo");
        Assert.True(r.IsSuccess);
        Assert.Equal(13, r.Value!.CharacterCountNoSpaces);
    }

    [Theory]
    [InlineData("hello world", 2)]
    [InlineData("one two three", 3)]
    [InlineData("a b c d e", 5)]
    [InlineData("Türkçe metin örneği", 3)]
    public void Count_word_count_uses_word_boundaries(string input, int expected)
    {
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value!.WordCount);
    }

    [Theory]
    [InlineData("Hello. World!", 2)]
    [InlineData("One sentence.", 1)]
    [InlineData("a.b.c.", 3)]
    [InlineData("No punctuation", 1)]
    public void Count_sentence_count_uses_terminators(string input, int expected)
    {
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value!.SentenceCount);
    }

    [Fact]
    public void Count_line_count_matches_newlines()
    {
        var r = _service.Count("line1\nline2\nline3");
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.LineCount);
    }

    [Fact]
    public void Count_paragraph_count_uses_double_newlines()
    {
        var r = _service.Count("paragraph 1\n\nparagraph 2\n\nparagraph 3");
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.ParagraphCount);
    }

    [Fact]
    public void Reading_time_is_based_on_200_wpm()
    {
        string input = string.Join(" ", System.Linq.Enumerable.Repeat("word", 200));
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(TimeSpan.FromMinutes(1), r.Value!.ReadingTime);
    }

    [Fact]
    public void Speaking_time_is_based_on_130_wpm()
    {
        string input = string.Join(" ", System.Linq.Enumerable.Repeat("word", 130));
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(TimeSpan.FromMinutes(1), r.Value!.SpeakingTime);
    }

    [Fact]
    public void Top_words_ignore_stop_words_and_short_words()
    {
        string input = "the quick brown fox jumps over the lazy dog the fox fox";
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);

        var top = r.Value!.TopWords;
        Assert.NotEmpty(top);
        Assert.Equal("fox", top[0].Word);
        Assert.Equal(3, top[0].Count);
    }

    [Fact]
    public void Top_words_returns_at_most_five_entries()
    {
        string input = "alpha beta gamma delta epsilon zeta eta theta iota kappa";
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.TopWords.Count <= 5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_input_returns_zero_report(string? input)
    {
        var r = _service.Count(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.CharacterCount);
        Assert.Equal(0, r.Value.WordCount);
        Assert.Empty(r.Value.TopWords);
    }

    [Fact]
    public void Unpaired_surrogate_is_reported()
    {
        var r = _service.Count("test\uD800");
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
        Assert.Equal(TextCounterErrorCodes.InvalidUnicode, r.ErrorCode);
    }

    [Fact]
    public void Too_large_input_is_reported()
    {
        string huge = new string('a', TextCounterService.MaxTextLengthLimit + 1);
        var r = _service.Count(huge);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
        Assert.Equal(TextCounterErrorCodes.InputTooLarge, r.ErrorCode);
    }
}