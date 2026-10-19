using tool_cantam.Services.ErrorCodes;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// HTML kodlayıcısının sözleşmesini kilitler: beş kritik karakterin kodlanması (tek tırnak dahil),
/// adlandırılmış/sayısal gösterim, ASCII dışı kodlama, varlık çözme ve HTML5 sayısal referans kuralları.
/// </summary>
public class HtmlEncoderServiceTests
{
    private readonly HtmlEncoderService _service = new();

    // ---------------------------------------------------------------------
    // Kodlama
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("a < b & c", "a &lt; b &amp; c")]
    [InlineData("<script>", "&lt;script&gt;")]
    [InlineData("5 > 3", "5 &gt; 3")]
    [InlineData("say \"hi\"", "say &quot;hi&quot;")]
    [InlineData("it's", "it&#39;s")]
    public void Encode_always_covers_the_five_critical_characters(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_output_never_contains_a_raw_dangerous_character()
    {
        const string hostile = "<img src=x onerror='alert(1)'>\"&";

        ToolResult<string> result = _service.Encode(hostile);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain('<', result.Value!);
        Assert.DoesNotContain('>', result.Value!);
        Assert.DoesNotContain('"', result.Value!);
        Assert.DoesNotContain('\'', result.Value!);
    }

    [Theory]
    [InlineData("a & b", "a &#38; b")]
    [InlineData("<b>", "&#60;b&#62;")]
    [InlineData("it's", "it&#39;s")]
    public void Encode_with_numeric_references_writes_decimal_escapes(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, useNamedEntities: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_keeps_non_ascii_by_default()
    {
        ToolResult<string> result = _service.Encode("café ğ 😀");

        Assert.True(result.IsSuccess);
        Assert.Equal("café ğ 😀", result.Value);
    }

    [Theory]
    [InlineData("café", "caf&#233;")]
    [InlineData("ğ", "&#287;")]
    [InlineData("İ", "&#304;")]
    [InlineData("😀", "&#128512;")]
    [InlineData("€", "&#8364;")]
    public void Encode_can_turn_non_ascii_into_numeric_references(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, encodeNonAscii: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_of_non_ascii_only_output_stays_ascii()
    {
        ToolResult<string> result = _service.Encode("Türkçe ğüşiöç 😀", encodeNonAscii: true);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!, character => Assert.True(character <= 0x7F, $"ASCII dışı karakter: {character}"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encode_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Encode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Encode_reports_unpaired_surrogates()
    {
        ToolResult<string> result = _service.Encode("\uD800");

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Encode_reports_too_large_input()
    {
        ToolResult<string> result = _service.Encode(new string('a', HtmlEncoderService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Çözme
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("&lt;script&gt;", "<script>")]
    [InlineData("&amp;", "&")]
    [InlineData("&quot;", "\"")]
    [InlineData("&#39;", "'")]
    [InlineData("&#x27;", "'")]
    [InlineData("&apos;", "'")]
    [InlineData("a &lt; b &amp;&amp; c &gt; d", "a < b && c > d")]
    public void Decode_reads_named_and_numeric_entities(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("&ccedil;", "ç")]
    [InlineData("&Uuml;", "Ü")]
    [InlineData("&nbsp;", "\u00A0")]
    [InlineData("&mdash;", "\u2014")]
    [InlineData("&euro;", "\u20AC")]
    [InlineData("&#233;", "é")]
    [InlineData("&#xE9;", "é")]
    public void Decode_reads_extended_entities(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Decode_applies_the_html5_legacy_windows1252_mapping()
    {
        // HTML5: &#150; en tire (U+2013) olarak yorumlanır, C1 kontrol karakteri olarak değil.
        ToolResult<string> result = _service.Decode("&#150;");

        Assert.True(result.IsSuccess);
        Assert.Equal("\u2013", result.Value);
    }

    [Fact]
    public void Decode_does_not_decode_twice()
    {
        ToolResult<string> result = _service.Decode("&amp;lt;");

        Assert.True(result.IsSuccess);
        Assert.Equal("&lt;", result.Value);
    }

    [Fact]
    public void Decode_leaves_plain_text_untouched()
    {
        const string plain = "Türkçe metin, hiç varlık yok.";

        ToolResult<string> result = _service.Decode(plain);

        Assert.True(result.IsSuccess);
        Assert.Equal(plain, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Decode_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    // ---------------------------------------------------------------------
    // Çözme: hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("&unknown;")]
    [InlineData("&madeupentity;")]
    public void Decode_reports_unknown_entities_in_strict_mode(string input)
    {
        ToolResult<string> result = _service.Decode(input, strictEntities: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.UnknownEntity, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("&unknown;")]
    [InlineData("&amp")]
    [InlineData("&;")]
    [InlineData("&#xZZ;")]
    public void Decode_keeps_the_original_text_in_tolerant_mode(string input)
    {
        ToolResult<string> result = _service.Decode(input, strictEntities: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(input, result.Value);
    }

    [Theory]
    [InlineData("&amp")]
    [InlineData("&lt")]
    [InlineData("&#39")]
    public void Decode_reports_missing_semicolons_in_strict_mode(string input)
    {
        ToolResult<string> result = _service.Decode(input, strictEntities: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.MissingSemicolon, result.ErrorCode);
    }

    [Theory]
    [InlineData("&;")]
    [InlineData("&#;")]
    [InlineData("&#x;")]
    [InlineData("&#xZZ;")]
    [InlineData("&#1a2;")]
    public void Decode_reports_malformed_numeric_references(string input)
    {
        ToolResult<string> result = _service.Decode(input, strictEntities: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.InvalidEntity, result.ErrorCode);
    }

    [Theory]
    [InlineData("&#0;")]
    [InlineData("&#xD800;")]
    [InlineData("&#55296;")]
    [InlineData("&#x110000;")]
    [InlineData("&#9999999999;")]
    public void Decode_reports_invalid_code_points(string input)
    {
        ToolResult<string> result = _service.Decode(input, strictEntities: true);

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.InvalidCodePoint, result.ErrorCode);
    }

    [Fact]
    public void Decode_reports_too_large_input()
    {
        ToolResult<string> result = _service.Decode(new string('a', HtmlEncoderService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(HtmlErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Gidiş-dönüş
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void Round_trip_returns_the_original_text(bool useNamedEntities, bool encodeNonAscii)
    {
        const string original = "<b>Türkçe</b> & \"alıntı\" 'tek' 😀 100%";

        ToolResult<string> encoded = _service.Encode(original, useNamedEntities, encodeNonAscii);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.Decode(encoded.Value!);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }
}
