using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hex kodlayıcısının sözleşmesini kilitler: biçim ve harf büyüklüğü, yaygın ayırıcı ve önekler,
/// tek basamak/geçersiz bayt hataları ve gidiş-dönüş kararlılığı.
/// </summary>
public class HexServiceTests
{
    private readonly HexService _service = new();

    // ---------------------------------------------------------------------
    // Kodlama
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Hello", "48656C6C6F")]
    [InlineData("Hi", "4869")]
    [InlineData("A", "41")]
    [InlineData("ğ", "C49F")]
    [InlineData("İ", "C4B0")]
    [InlineData("😀", "F09F9880")]
    public void Encode_writes_uppercase_compact_hex_by_default(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_honours_lowercase_option()
    {
        ToolResult<string> result = _service.Encode("Hello", HexFormat.Compact, uppercase: false);

        Assert.True(result.IsSuccess);
        Assert.Equal("48656c6c6f", result.Value);
    }

    [Theory]
    [InlineData(HexFormat.Compact, "48656C6C6F")]
    [InlineData(HexFormat.Spaced, "48 65 6C 6C 6F")]
    [InlineData(HexFormat.ColonSeparated, "48:65:6C:6C:6F")]
    public void Encode_honours_the_selected_format(HexFormat format, string expected)
    {
        ToolResult<string> result = _service.Encode("Hello", format);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_of_a_single_byte_has_no_trailing_separator()
    {
        Assert.Equal("41", _service.Encode("A", HexFormat.Spaced).Value);
        Assert.Equal("41", _service.Encode("A", HexFormat.ColonSeparated).Value);
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
        Assert.Equal(HexErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Encode_reports_too_large_input()
    {
        ToolResult<string> result = _service.Encode(new string('a', HexService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(HexErrorCodes.InputTooLarge, result.ErrorCode);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
    }

    // ---------------------------------------------------------------------
    // Çözme: biçimler ve ayırıcılar
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("48656C6C6F", "Hello")]
    [InlineData("48656c6c6f", "Hello")]
    [InlineData("48 65 6C 6C 6F", "Hello")]
    [InlineData("48:65:6C:6C:6F", "Hello")]
    [InlineData("48,65,6C,6C,6F", "Hello")]
    [InlineData("48-65-6C-6C-6F", "Hello")]
    [InlineData("48_65_6C_6C_6F", "Hello")]
    [InlineData("48\n65\n6C\n6C\n6F", "Hello")]
    [InlineData("  48656C6C6F  ", "Hello")]
    [InlineData("0x48 0x65 0x6C 0x6C 0x6F", "Hello")]
    [InlineData("\\x48\\x65\\x6C\\x6C\\x6F", "Hello")]
    [InlineData("0X48 0X69", "Hi")]
    public void Decode_accepts_common_hex_notations(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Decode_reads_turkish_and_emoji_text()
    {
        Assert.Equal("ğ", _service.Decode("C49F").Value);
        Assert.Equal("😀", _service.Decode("F09F9880").Value);
        Assert.Equal("Türkçe", _service.Decode(_service.Encode("Türkçe").Value!).Value);
    }

    [Theory]
    [InlineData(HexFormat.Compact)]
    [InlineData(HexFormat.Spaced)]
    [InlineData(HexFormat.ColonSeparated)]
    public void Round_trip_returns_the_original_text(HexFormat format)
    {
        const string original = "Türkçe metin 😀 100%";

        ToolResult<string> encoded = _service.Encode(original, format);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.Decode(encoded.Value!);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" : ")]
    public void Decode_returns_empty_for_blank_input(string? input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    // ---------------------------------------------------------------------
    // Hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("ABC")]          // eskiden son basamak sessizce atılıyordu
    [InlineData("48656C6C6")]
    [InlineData("48 6")]
    [InlineData("4")]
    [InlineData("0x4")]
    public void Decode_reports_odd_number_of_digits_instead_of_dropping_the_last_one(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(HexErrorCodes.OddLength, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("48 6G")]
    [InlineData("ZZ")]
    [InlineData("48-6Z")]
    [InlineData("\\48")]
    [InlineData("48.65")]
    public void Decode_reports_invalid_digits(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(HexErrorCodes.InvalidDigit, result.ErrorCode);
    }

    [Theory]
    [InlineData("FF")]        // UTF-8'de geçersiz bayt
    [InlineData("C3")]        // yarım kalmış dizi
    [InlineData("E282")]      // eksik devam baytı
    [InlineData("EDA080")]    // surrogate kodlaması UTF-8'de geçersizdir
    public void Decode_reports_invalid_utf8_instead_of_replacing_characters(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(HexErrorCodes.NotUtf8, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    [Fact]
    public void Decode_reports_too_large_input()
    {
        ToolResult<string> result = _service.Decode(new string('A', HexService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(HexErrorCodes.InputTooLarge, result.ErrorCode);
    }
}
