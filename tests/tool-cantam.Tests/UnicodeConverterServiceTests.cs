using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Unicode kaçış dönüştürücüsünün sözleşmesini kilitler: dört kaçış biçimi, vekil çifti üretimi,
/// kaçış çözme (tüm biçimler + C kaçışları) ve bozuk girdinin reddi.
/// </summary>
public class UnicodeConverterServiceTests
{
    private readonly UnicodeConverterService _service = new();

    // ---------------------------------------------------------------------
    // Kodlama
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("A", "\\u0041")]
    [InlineData("ğ", "\\u011F")]
    [InlineData("€", "\\u20AC")]
    [InlineData("AB", "\\u0041\\u0042")]
    [InlineData("😀", "\\uD83D\\uDE00")]
    public void Escape_writes_utf16_escapes_with_surrogate_pairs_for_astral_characters(string input, string expected)
    {
        ToolResult<string> result = _service.Escape(input, UnicodeEscapeFormat.Utf16Escape);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("A", "\\U00000041")]
    [InlineData("ğ", "\\U0000011F")]
    [InlineData("😀", "\\U0001F600")]
    public void Escape_writes_eight_digit_code_point_escapes(string input, string expected)
    {
        ToolResult<string> result = _service.Escape(input, UnicodeEscapeFormat.CodePointEscape);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("A", "\\x41")]
    [InlineData("ü", "\\xC3\\xBC")]
    [InlineData("€", "\\xE2\\x82\\xAC")]
    [InlineData("😀", "\\xF0\\x9F\\x98\\x80")]
    public void Escape_writes_utf8_bytes(string input, string expected)
    {
        ToolResult<string> result = _service.Escape(input, UnicodeEscapeFormat.Utf8ByteEscape);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("A", "U+0041")]
    [InlineData("ğ", "U+011F")]
    [InlineData("😀", "U+1F600")]
    public void Escape_writes_readable_code_point_notation(string input, string expected)
    {
        ToolResult<string> result = _service.Escape(input, UnicodeEscapeFormat.CodePointNotation);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Escape_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Escape(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Escape_reports_unpaired_surrogates_in_the_input()
    {
        ToolResult<string> result = _service.Escape("\uD800");

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Escape_reports_too_large_input()
    {
        ToolResult<string> result = _service.Escape(new string('a', UnicodeConverterService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Çözme
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("\\u0041", "A")]
    [InlineData("\\u011F", "ğ")]
    [InlineData("\\uD83D\\uDE00", "😀")]
    [InlineData("\\u{1F600}", "😀")]
    [InlineData("\\u{41}", "A")]
    [InlineData("\\U0001F600", "😀")]
    [InlineData("\\U00000041", "A")]
    [InlineData("\\x41", "A")]
    [InlineData("\\xE2\\x82\\xAC", "€")]
    [InlineData("\\xC3\\xBC", "ü")]
    [InlineData("U+0041", "A")]
    [InlineData("u+1f600", "😀")]
    public void Unescape_reads_every_supported_notation(string input, string expected)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("\\n", "\n")]
    [InlineData("\\t", "\t")]
    [InlineData("\\\\", "\\")]
    [InlineData("\\\"", "\"")]
    public void Unescape_reads_common_c_escapes(string input, string expected)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Unescape_mixes_literal_text_and_escapes()
    {
        ToolResult<string> result = _service.Unescape("Merhaba \\u0041 ve \\x42!");

        Assert.True(result.IsSuccess);
        Assert.Equal("Merhaba A ve B!", result.Value);
    }

    [Fact]
    public void Unescape_leaves_text_without_escapes_untouched()
    {
        const string plain = "Türkçe metin, kaçış yok 😀";

        ToolResult<string> result = _service.Unescape(plain);

        Assert.True(result.IsSuccess);
        Assert.Equal(plain, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Unescape_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    // ---------------------------------------------------------------------
    // Hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("\\uD83D")]                 // tek başına yüksek vekil
    [InlineData("\\uDE00")]                 // tek başına alçak vekil
    [InlineData("\\uD83D\\u0041")]          // yüksek vekil + vekil olmayan kaçış
    [InlineData("\\uD83D ve \\uDE00")]      // araya metin girmiş çift
    public void Unescape_reports_lone_surrogates(string input)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.LoneSurrogate, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("\\u00")]                   // eksik basamak
    [InlineData("\\uZZZZ")]
    [InlineData("\\U0001F60")]              // eksik basamak
    [InlineData("\\x4")]
    [InlineData("\\xZZ")]
    [InlineData("\\q")]                     // bilinmeyen kaçış
    [InlineData("abc\\")]                   // sonda kalan ters eğik çizgi
    [InlineData("\\u{1F600")]               // kapanmamış süslü parantez
    public void Unescape_reports_malformed_escapes(string input)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.InvalidEscape, result.ErrorCode);
    }

    [Theory]
    [InlineData("\\U00110000")]             // Unicode dışı
    [InlineData("\\U0000D800")]             // vekil aralık
    [InlineData("\\u{110000}")]
    [InlineData("\\u{D800}")]
    public void Unescape_reports_invalid_code_points(string input)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.InvalidCodePoint, result.ErrorCode);
    }

    [Theory]
    [InlineData("\\xE2")]                   // yarım kalmış UTF-8 dizisi
    [InlineData("\\xFF")]
    [InlineData("\\xE2\\x82")]
    public void Unescape_reports_invalid_utf8_byte_sequences(string input)
    {
        ToolResult<string> result = _service.Unescape(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.NotUtf8, result.ErrorCode);
    }

    [Fact]
    public void Unescape_reports_too_large_input()
    {
        ToolResult<string> result = _service.Unescape(new string('a', UnicodeConverterService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(UnicodeErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Gidiş-dönüş
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(UnicodeEscapeFormat.Utf16Escape)]
    [InlineData(UnicodeEscapeFormat.CodePointEscape)]
    [InlineData(UnicodeEscapeFormat.Utf8ByteEscape)]
    [InlineData(UnicodeEscapeFormat.CodePointNotation)]
    public void Round_trip_returns_the_original_text(UnicodeEscapeFormat format)
    {
        const string original = "Türkçe ğüşıöç 😀 € 100% \\ backslash";

        ToolResult<string> escaped = _service.Escape(original, format);
        Assert.True(escaped.IsSuccess);

        ToolResult<string> text = _service.Unescape(escaped.Value!);
        Assert.True(text.IsSuccess);
        Assert.Equal(original, text.Value);
    }
}
