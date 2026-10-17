using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// URL kodlayıcısının sözleşmesini kilitler: RFC 3986 unreserved kümesi, bağlama göre
/// boşluk davranışı, katı kaçış/UTF-8 doğrulaması ve gidiş-dönüş kararlılığı.
/// </summary>
public class UrlServiceTests
{
    private readonly UrlService _service = new();

    // ---------------------------------------------------------------------
    // Kodlama: bileşen bağlamı
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("abcXYZ019", "abcXYZ019")]
    [InlineData("-._~", "-._~")]
    [InlineData("a b", "a%20b")]
    [InlineData("a+b", "a%2Bb")]
    [InlineData("a&b=c", "a%26b%3Dc")]
    [InlineData("/path?q=1", "%2Fpath%3Fq%3D1")]
    [InlineData("100%", "100%25")]
    [InlineData("a#b", "a%23b")]
    public void Encode_component_keeps_only_unreserved_characters(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("café", "caf%C3%A9")]
    [InlineData("ğ", "%C4%9F")]
    [InlineData("İ", "%C4%B0")]
    [InlineData("€", "%E2%82%AC")]
    [InlineData("😀", "%F0%9F%98%80")]
    public void Encode_component_writes_uppercase_utf8_escapes(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    // ---------------------------------------------------------------------
    // Kodlama: form gövdesi ve tam URL
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("a b", "a+b")]
    [InlineData("a+b", "a%2Bb")]
    [InlineData("ad soyad", "ad+soyad")]
    [InlineData("a~b", "a~b")]
    public void Encode_form_uses_plus_for_space(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, UrlEncodingMode.Form);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("https://x.test/a b?q=1&r=2", "https://x.test/a%20b?q=1&r=2")]
    [InlineData("https://x.test/p#frag", "https://x.test/p#frag")]
    [InlineData("a+b", "a+b")]
    [InlineData("a/b?c=d&e=f", "a/b?c=d&e=f")]
    public void Encode_full_url_keeps_structure_characters(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, UrlEncodingMode.FullUrl);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    // ---------------------------------------------------------------------
    // Çözme
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("a%20b", "a b")]
    [InlineData("caf%C3%A9", "café")]
    [InlineData("%F0%9F%98%80", "😀")]
    [InlineData("100%25", "100%")]
    [InlineData("%c3%bc", "ü")]                       // küçük harfli kaçışlar da geçerli
    [InlineData("plain", "plain")]                    // kaçışsız metin olduğu gibi geçer
    [InlineData("café", "café")]                      // kaçışlanmamış Unicode korunur
    public void Decode_component_reads_escapes_and_keeps_plus_literal(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input, UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Decode_component_does_not_turn_plus_into_space()
    {
        ToolResult<string> result = _service.Decode("a+b", UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal("a+b", result.Value);
    }

    [Theory]
    [InlineData("a+b", "a b")]
    [InlineData("a%2Bb", "a+b")]
    [InlineData("a%20b", "a b")]
    [InlineData("ad+soyad", "ad soyad")]
    public void Decode_form_turns_plus_into_space(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input, UrlEncodingMode.Form);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Decode_full_url_does_not_turn_plus_into_space()
    {
        ToolResult<string> result = _service.Decode("https://x.test/a%20b?q=1&r=2", UrlEncodingMode.FullUrl);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://x.test/a b?q=1&r=2", result.Value);
    }

    // ---------------------------------------------------------------------
    // Gidiş-dönüş
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(UrlEncodingMode.Component)]
    [InlineData(UrlEncodingMode.Form)]
    [InlineData(UrlEncodingMode.FullUrl)]
    public void Round_trip_returns_the_original_text(UrlEncodingMode mode)
    {
        const string original = "Türkçe metin & semboller: a+b=c? 100% 😀 café";

        ToolResult<string> encoded = _service.Encode(original, mode);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.Decode(encoded.Value!, mode);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }

    // ---------------------------------------------------------------------
    // Boş girdi ve sınırlar
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Encode_returns_empty_for_empty_input(string? input)
    {
        ToolResult<string> result = _service.Encode(input, UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decode_returns_empty_for_blank_input(string? input)
    {
        ToolResult<string> result = _service.Decode(input, UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void Encode_reports_too_large_input()
    {
        string oversized = new('a', UrlService.MaxTextLengthLimit + 1);

        ToolResult<string> result = _service.Encode(oversized, UrlEncodingMode.Component);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
        Assert.Equal(UrlErrorCodes.InputTooLarge, result.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // Hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("%")]
    [InlineData("%4")]
    [InlineData("%zz")]
    [InlineData("abc%")]
    [InlineData("a%G0")]
    public void Decode_reports_invalid_percent_escape(string input)
    {
        ToolResult<string> result = _service.Decode(input, UrlEncodingMode.Component);

        Assert.False(result.IsSuccess);
        Assert.Equal(UrlErrorCodes.InvalidEscape, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("%FF")]         // UTF-8'de geçersiz bayt
    [InlineData("%C3")]         // yarım kalmış çok baytlı dizi
    [InlineData("%E2%82")]      // eksik devam baytı
    [InlineData("%ED%A0%80")]   // surrogate kodlaması UTF-8'de geçersizdir
    public void Decode_reports_invalid_utf8_instead_of_replacing_characters(string input)
    {
        ToolResult<string> result = _service.Decode(input, UrlEncodingMode.Component);

        Assert.False(result.IsSuccess);
        Assert.Equal(UrlErrorCodes.NotUtf8, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    [Fact]
    public void Encode_reports_unpaired_surrogates_instead_of_silently_replacing_them()
    {
        ToolResult<string> high = _service.Encode("\uD800", UrlEncodingMode.Component);
        Assert.False(high.IsSuccess);
        Assert.Equal(UrlErrorCodes.InvalidUnicode, high.ErrorCode);

        ToolResult<string> low = _service.Encode("a\uDC00b", UrlEncodingMode.Component);
        Assert.False(low.IsSuccess);
        Assert.Equal(UrlErrorCodes.InvalidUnicode, low.ErrorCode);

        ToolResult<string> trailingHigh = _service.Encode("ab\uD83D", UrlEncodingMode.Component);
        Assert.False(trailingHigh.IsSuccess);
        Assert.Equal(UrlErrorCodes.InvalidUnicode, trailingHigh.ErrorCode);
    }

    [Fact]
    public void Decode_accepts_a_valid_surrogate_pair_written_directly()
    {
        // Kaçışlanmamış emoji doğrudan girdide bulunabilir; bu geçerli bir çiftir.
        ToolResult<string> result = _service.Decode("😀", UrlEncodingMode.Component);

        Assert.True(result.IsSuccess);
        Assert.Equal("😀", result.Value);
    }
}
