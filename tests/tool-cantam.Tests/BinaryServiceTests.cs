using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// İkili kodlayıcının sözleşmesini kilitler: bit düzeni, biçim ve önek seçenekleri,
/// eksik bayt hatası, geçersiz karakterler ve gidiş-dönüş kararlılığı.
/// </summary>
public class BinaryServiceTests
{
    private readonly BinaryService _service = new();

    // ---------------------------------------------------------------------
    // Kodlama
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("A", "01000001")]
    [InlineData("Hi", "0100100001101001")]
    [InlineData(" ", "00100000")]
    public void Encode_writes_one_byte_per_eight_bits(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_with_spaced_format_separates_bytes()
    {
        ToolResult<string> result = _service.Encode("Hi", BinaryFormat.Spaced);

        Assert.True(result.IsSuccess);
        Assert.Equal("01001000 01101001", result.Value);
    }

    [Fact]
    public void Encode_with_byte_prefix_marks_every_byte()
    {
        ToolResult<string> result = _service.Encode("Hi", BinaryFormat.Spaced, includeBytePrefix: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("0b01001000 0b01101001", result.Value);
    }

    [Theory]
    [InlineData("ğ", "11000100 10011111")]
    [InlineData("İ", "11000100 10110000")]
    public void Encode_writes_utf8_sequences(string input, string expected)
    {
        ToolResult<string> result = _service.Encode(input, BinaryFormat.Spaced);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Encode_writes_four_bytes_for_an_emoji()
    {
        ToolResult<string> result = _service.Encode("😀", BinaryFormat.Spaced);

        Assert.True(result.IsSuccess);
        Assert.Equal("11110000 10011111 10011000 10000000", result.Value);
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
        Assert.Equal(BinaryErrorCodes.InvalidUnicode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Encode_reports_when_the_representation_would_exceed_the_limit()
    {
        // 1,3 milyon ASCII karakter: bitişik gösterim 10 milyon karakteri aşar.
        string input = new('a', (BinaryService.MaxTextLengthLimit / 8) + 1);

        ToolResult<string> result = _service.Encode(input, BinaryFormat.Compact);

        Assert.False(result.IsSuccess);
        Assert.Equal(BinaryErrorCodes.InputTooLarge, result.ErrorCode);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
    }

    [Fact]
    public void Encode_stays_within_the_limit_for_a_small_input()
    {
        ToolResult<string> result = _service.Encode("merhaba");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Length <= BinaryService.MaxTextLengthLimit);
    }

    // ---------------------------------------------------------------------
    // Çözme
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("01000001", "A")]
    [InlineData("0100100001101001", "Hi")]
    [InlineData("01001000 01101001", "Hi")]
    [InlineData("01001000_01101001", "Hi")]
    [InlineData("01001000\n01101001", "Hi")]
    [InlineData("0b01001000 0b01101001", "Hi")]
    [InlineData("0B01001000 0B01101001", "Hi")]
    [InlineData("  01000001  ", "A")]
    public void Decode_accepts_common_binary_notations(string input, string expected)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Decode_reads_turkish_and_emoji_text()
    {
        Assert.Equal("ğ", _service.Decode("11000100 10011111").Value);
        Assert.Equal("😀", _service.Decode("11110000 10011111 10011000 10000000").Value);
    }

    [Theory]
    [InlineData(BinaryFormat.Compact, false)]
    [InlineData(BinaryFormat.Spaced, false)]
    [InlineData(BinaryFormat.Compact, true)]
    [InlineData(BinaryFormat.Spaced, true)]
    public void Round_trip_returns_the_original_text(BinaryFormat format, bool includeBytePrefix)
    {
        const string original = "Türkçe metin 😀 100%";

        ToolResult<string> encoded = _service.Encode(original, format, includeBytePrefix);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.Decode(encoded.Value!);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
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
    [InlineData("0100000")]        // 7 bit: eskiden sessizce atılıyordu
    [InlineData("010000011")]      // 9 bit
    [InlineData("01001000 011")]   // eksik bayt
    [InlineData("1")]
    [InlineData("0b0100000")]
    public void Decode_reports_a_bit_count_that_is_not_a_multiple_of_eight(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(BinaryErrorCodes.OddBitCount, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("01000002")]
    [InlineData("0100 000a")]
    [InlineData("x0100001")]
    [InlineData("01000001!")]
    public void Decode_reports_invalid_characters(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(BinaryErrorCodes.InvalidDigit, result.ErrorCode);
    }

    [Theory]
    [InlineData("11111111")]                       // UTF-8'de geçersiz bayt
    [InlineData("11000011")]                       // yarım kalmış iki baytlı dizi
    [InlineData("11101101 10100000 10000000")]     // surrogate kodlaması geçersizdir
    public void Decode_reports_invalid_utf8_instead_of_replacing_characters(string input)
    {
        ToolResult<string> result = _service.Decode(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(BinaryErrorCodes.NotUtf8, result.ErrorCode);
    }

    [Fact]
    public void Decode_reports_too_large_input()
    {
        ToolResult<string> result = _service.Decode(new string('0', BinaryService.MaxTextLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(BinaryErrorCodes.InputTooLarge, result.ErrorCode);
    }
}
