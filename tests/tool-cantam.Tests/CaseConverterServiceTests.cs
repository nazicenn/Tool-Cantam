using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Harf dönüşüm servisinin sözleşmesini kilitler: her mod için bilinen giriş/çıkış,
/// Unicode davranışı, boyut sınırı ve geçersiz giriş yolları.
/// </summary>
public class CaseConverterServiceTests
{
    private readonly CaseConverterService _service = new();

    // ---------------------------------------------------------------------
    // Upper / Lower
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("hello world", "HELLO WORLD")]
    [InlineData("Türkçe ıİ", "TÜRKÇE Iİ")]   // InvariantCulture: i→I, İ→İ
    public void ToUpper_produces_invariant_uppercase(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Upper);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Fact]
    public void ToLower_produces_invariant_lowercase()
    {
        var r = _service.Convert("HELLO World", CaseConversionMode.Lower);
        Assert.True(r.IsSuccess);
        Assert.Equal("hello world", r.Value);
    }

    // ---------------------------------------------------------------------
    // Title / Sentence
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO world", "Hello World")]
    [InlineData("foo-bar baz", "Foo-Bar Baz")]
    public void ToTitle_capitalizes_each_word(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Title);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("hello world. goodbye world.", "Hello world. Goodbye world.")]
    [InlineData("HELLO. WORLD!", "Hello. World!")]
    public void ToSentence_capitalizes_sentence_starts(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Sentence);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    // ---------------------------------------------------------------------
    // Camel / Pascal / Snake / Kebab
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("hello world", "helloWorld")]
    [InlineData("Hello World Test", "helloWorldTest")]
    [InlineData("foo_bar-baz", "fooBarBaz")]
    public void ToCamel_joins_words(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Camel);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("foo_bar-baz", "FooBarBaz")]
    public void ToPascal_joins_words_with_capitals(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Pascal);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("Hello World", "hello_world")]
    [InlineData("foo-bar baz", "foo_bar_baz")]
    public void ToSnake_joins_with_underscore(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Snake);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("foo_bar baz", "foo-bar-baz")]
    public void ToKebab_joins_with_dash(string input, string expected)
    {
        var r = _service.Convert(input, CaseConversionMode.Kebab);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    // ---------------------------------------------------------------------
    // Alternating / Invert
    // ---------------------------------------------------------------------

    [Fact]
    public void ToAlternating_alternates_letter_case()
    {
        var r = _service.Convert("hello", CaseConversionMode.Alternating);
        Assert.True(r.IsSuccess);
        Assert.Equal("HeLlO", r.Value);
    }

    [Fact]
    public void ToInvert_swaps_case()
    {
        var r = _service.Convert("Hello World", CaseConversionMode.Invert);
        Assert.True(r.IsSuccess);
        Assert.Equal("hELLO wORLD", r.Value);
    }

    [Fact]
    public void ToInvert_preserves_emoji_and_symbols()
    {
        var r = _service.Convert("Hello 😀 World!", CaseConversionMode.Invert);
        Assert.True(r.IsSuccess);
        Assert.Contains("😀", r.Value);
        Assert.Contains("!", r.Value);
    }

    // ---------------------------------------------------------------------
    // Edge cases
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_input_returns_empty(string? input)
    {
        var r = _service.Convert(input, CaseConversionMode.Upper);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Unpaired_surrogate_is_reported()
    {
        var r = _service.Convert("test\uD800", CaseConversionMode.Upper);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
        Assert.Equal(CaseConverterErrorCodes.InvalidUnicode, r.ErrorCode);
    }

    [Fact]
    public void Too_large_input_is_reported()
    {
        string huge = new string('a', CaseConverterService.MaxTextLengthLimit + 1);
        var r = _service.Convert(huge, CaseConversionMode.Upper);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
        Assert.Equal(CaseConverterErrorCodes.InputTooLarge, r.ErrorCode);
    }

    [Fact]
    public void Supported_modes_contains_all_ten_modes()
    {
        Assert.Equal(10, _service.SupportedModes.Count);
        Assert.Contains(CaseConversionMode.Upper, _service.SupportedModes);
        Assert.Contains(CaseConversionMode.Invert, _service.SupportedModes);
    }
}