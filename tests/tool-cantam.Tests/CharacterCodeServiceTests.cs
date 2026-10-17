using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Karakter kodu dönüştürücüsünün sözleşmesini kilitler: kod noktası tabanlı dönüşüm (emoji tek
/// sayıdır), ondalık/onaltılık biçimler, ayırıcı ve önek kabulü, geçersiz kod noktası reddi.
/// </summary>
public class CharacterCodeServiceTests
{
    private readonly AsciiConverterService _service = new();

    // ---------------------------------------------------------------------
    // Metin -> kod
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("ABC", "65 66 67")]
    [InlineData("A", "65")]
    [InlineData(" ", "32")]
    [InlineData("~", "126")]
    [InlineData("ğ", "287")]
    [InlineData("İ", "304")]
    [InlineData("€", "8364")]
    [InlineData("😀", "128512")]
    public void ToCodes_writes_unicode_code_points_in_decimal(string input, string expected)
    {
        ToolResult<string> result = _service.ToCodes(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ToCodes_treats_an_emoji_as_a_single_value()
    {
        // Eskiden UTF-16 kod birimleri yazılıyordu: 55357 56832
        ToolResult<string> result = _service.ToCodes("😀");

        Assert.True(result.IsSuccess);
        Assert.Equal("128512", result.Value);
        Assert.DoesNotContain("55357", result.Value!);
    }

    [Theory]
    [InlineData("A", "U+0041")]
    [InlineData("ğ", "U+011F")]
    [InlineData("€", "U+20AC")]
    [InlineData("😀", "U+1F600")]
    [InlineData("AB", "U+0041 U+0042")]
    public void ToCodes_writes_hexadecimal_with_a_prefix(string input, string expected)
    {
        ToolResult<string> result = _service.ToCodes(input, CharacterCodeFormat.Hexadecimal);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToCodes_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.ToCodes(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void ToCodes_reports_unpaired_surrogates()
    {
        ToolResult<string> result = _service.ToCodes("\uD800");

        Assert.False(result.IsSuccess);
        Assert.Equal(CharacterCodeErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void ToCodes_reports_too_large_input()
    {
        ToolResult<string> result = _service.ToCodes(new string('a', AsciiConverterService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CharacterCodeErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Kod -> metin
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("65 66 67", "ABC")]
    [InlineData("65,66;67", "ABC")]
    [InlineData("65|66|67", "ABC")]
    [InlineData("65\n66\t67", "ABC")]
    [InlineData("  65   66   67  ", "ABC")]
    [InlineData("128512", "😀")]
    [InlineData("287 304", "ğİ")]
    public void ToText_reads_decimal_values(string input, string expected)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("U+0041 U+0042", "AB")]
    [InlineData("u+0041", "A")]
    [InlineData("0x41 0x42", "AB")]
    [InlineData("0X41", "A")]
    [InlineData("\\u0041 \\u0042", "AB")]
    [InlineData("U+1F600", "😀")]
    public void ToText_reads_prefixed_hexadecimal_values(string input, string expected)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ToText_uses_the_selected_format_for_values_without_a_prefix()
    {
        // Aynı girdi, biçime göre farklı sonuç verir: 41 ondalıkta ')' , onaltılıkta 'A'.
        Assert.Equal(")*", _service.ToText("41 42", CharacterCodeFormat.Decimal).Value);
        Assert.Equal("AB", _service.ToText("41 42", CharacterCodeFormat.Hexadecimal).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(", ; |")]
    public void ToText_returns_empty_for_blank_input(string? input)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    // ---------------------------------------------------------------------
    // Hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("abc")]
    [InlineData("65 x")]
    [InlineData("65 6a")]
    [InlineData("U+00ZZ")]
    [InlineData("-1")]
    public void ToText_reports_tokens_that_are_not_numbers(string input)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(CharacterCodeErrorCodes.InvalidToken, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("55296")]        // U+D800: vekil aralığın başı
    [InlineData("57343")]        // U+DFFF: vekil aralığın sonu
    [InlineData("0xD800")]
    [InlineData("U+DFFF")]
    [InlineData("1114112")]      // U+110000: Unicode dışı
    [InlineData("U+110000")]
    [InlineData("99999999999")]  // int aralığının dışında
    public void ToText_rejects_invalid_code_points_instead_of_producing_broken_text(string input)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(CharacterCodeErrorCodes.InvalidCodePoint, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65")]
    [InlineData("1114111")]      // U+10FFFF: en büyük geçerli kod noktası
    public void ToText_accepts_the_full_valid_range(string input)
    {
        ToolResult<string> result = _service.ToText(input);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Value));
    }

    [Fact]
    public void ToText_reports_too_large_input()
    {
        ToolResult<string> result = _service.ToText(new string('6', AsciiConverterService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CharacterCodeErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Gidiş-dönüş
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(CharacterCodeFormat.Decimal)]
    [InlineData(CharacterCodeFormat.Hexadecimal)]
    public void Round_trip_returns_the_original_text(CharacterCodeFormat format)
    {
        const string original = "Türkçe ğüşıöç İ 😀 € 100% ASCII";

        ToolResult<string> codes = _service.ToCodes(original, format);
        Assert.True(codes.IsSuccess);

        ToolResult<string> text = _service.ToText(codes.Value!, format);
        Assert.True(text.IsSuccess);
        Assert.Equal(original, text.Value);
    }
}
