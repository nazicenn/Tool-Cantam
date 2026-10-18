using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class AsciiConverterServiceTests
{
    private readonly AsciiConverterService _service = new();

    // ---------- ToCodes ----------

    [Fact]
    public void ToCodes_Decimal_Simple()
    {
        var r = _service.ToCodes("AB", CharacterCodeFormat.Decimal);
        Assert.True(r.IsSuccess);
        Assert.Equal("65 66", r.Value);
    }

    [Fact]
    public void ToCodes_Hex_Simple()
    {
        var r = _service.ToCodes("A", CharacterCodeFormat.Hexadecimal);
        Assert.True(r.IsSuccess);
        Assert.Contains("U+0041", r.Value);
    }

    [Fact]
    public void ToCodes_Emoji_SingleCodePoint()
    {
        var r = _service.ToCodes("😀", CharacterCodeFormat.Decimal);
        Assert.True(r.IsSuccess);
        // 1F600 = 128512 (tek kod noktası, 2 vekil değil)
        Assert.Equal("128512", r.Value);
    }

    [Fact]
    public void ToCodes_Null_ReturnsOk()
    {
        var r = _service.ToCodes(null);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void ToCodes_Empty_ReturnsOk()
    {
        var r = _service.ToCodes("");
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void ToCodes_TooLarge_ReturnsTooLarge()
    {
        var huge = new string('a', _service.MaxTextLength + 1);
        var r = _service.ToCodes(huge);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
    }

    // ---------- ToText ----------

    [Fact]
    public void ToText_Decimal_Simple()
    {
        var r = _service.ToText("65 66", CharacterCodeFormat.Decimal);
        Assert.True(r.IsSuccess);
        Assert.Equal("AB", r.Value);
    }

    [Fact]
    public void ToText_HexWithUPrefix_Simple()
    {
        var r = _service.ToText("U+0041", CharacterCodeFormat.Hexadecimal);
        Assert.True(r.IsSuccess);
        Assert.Equal("A", r.Value);
    }

    [Fact]
    public void ToText_EmojiCodePoint_Works()
    {
        var r = _service.ToText("128512", CharacterCodeFormat.Decimal);
        Assert.True(r.IsSuccess);
        Assert.Equal("😀", r.Value);
    }

    [Fact]
    public void ToText_CommaSeparated_Works()
    {
        var r = _service.ToText("65,66", CharacterCodeFormat.Decimal);
        Assert.True(r.IsSuccess);
        Assert.Equal("AB", r.Value);
    }

    [Fact]
    public void ToText_InvalidToken_ReturnsInvalidFormat()
    {
        var r = _service.ToText("abc", CharacterCodeFormat.Decimal);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void ToText_InvalidCodePoint_ReturnsInvalidFormat()
    {
        // Surrogate range (0xD800-0xDFFF) reddedilmeli
        var r = _service.ToText("55296", CharacterCodeFormat.Decimal);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void ToText_OutOfRange_ReturnsInvalidFormat()
    {
        var r = _service.ToText("99999999", CharacterCodeFormat.Decimal);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void ToText_Null_ReturnsOk()
    {
        var r = _service.ToText(null);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }
}