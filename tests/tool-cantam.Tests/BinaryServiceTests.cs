using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class BinaryServiceTests
{
    private readonly BinaryService _service = new();

    // ---------- Encode ----------

    [Fact]
    public void Encode_Compact_Simple()
    {
        var r = _service.Encode("Hi", BinaryFormat.Compact);
        Assert.True(r.IsSuccess);
        Assert.Equal("0100100001101001", r.Value);
    }

    [Fact]
    public void Encode_Spaced_Simple()
    {
        var r = _service.Encode("Hi", BinaryFormat.Spaced);
        Assert.True(r.IsSuccess);
        Assert.Equal("01001000 01101001", r.Value);
    }

    [Fact]
    public void Encode_WithPrefix_AddsZeroB()
    {
        var r = _service.Encode("A", BinaryFormat.Spaced, includeBytePrefix: true);
        Assert.True(r.IsSuccess);
        Assert.Contains("0b", r.Value);
    }

    [Fact]
    public void Encode_Empty_ReturnsEmpty()
    {
        var r = _service.Encode("");
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Encode_Null_ReturnsEmpty()
    {
        var r = _service.Encode(null);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Encode_TooLarge_ReturnsTooLarge()
    {
        var huge = new string('a', _service.MaxTextLength + 1);
        var r = _service.Encode(huge);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
    }

    // ---------- Decode ----------

    [Fact]
    public void Decode_Compact_Simple()
    {
        var r = _service.Decode("0100100001101001");
        Assert.True(r.IsSuccess);
        Assert.Equal("Hi", r.Value);
    }

    [Fact]
    public void Decode_Spaced_Simple()
    {
        var r = _service.Decode("01001000 01101001");
        Assert.True(r.IsSuccess);
        Assert.Equal("Hi", r.Value);
    }

    [Fact]
    public void Decode_WithPrefix_Works()
    {
        var r = _service.Decode("0b01001000 0b01101001");
        Assert.True(r.IsSuccess);
        Assert.Equal("Hi", r.Value);
    }

    [Fact]
    public void Decode_Underscore_Ignored()
    {
        var r = _service.Decode("01001000_01101001");
        Assert.True(r.IsSuccess);
        Assert.Equal("Hi", r.Value);
    }

    [Fact]
    public void Decode_InvalidDigit_ReturnsInvalidFormat()
    {
        var r = _service.Decode("01001002");
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void Decode_OddBitCount_ReturnsInvalidFormat()
    {
        var r = _service.Decode("0100100");
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        var r = _service.Decode("");
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Decode_Null_ReturnsEmpty()
    {
        var r = _service.Decode(null);
        Assert.True(r.IsSuccess);
        Assert.Equal(string.Empty, r.Value);
    }

    [Fact]
    public void Decode_NotUtf8_ReturnsInvalidFormat()
    {
        // 11111111 = 0xFF, geçerli UTF-8 değil
        var r = _service.Decode("11111111");
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, r.ErrorKind);
    }

    [Fact]
    public void EncodeThenDecode_RoundTrip()
    {
        var original = "Merhaba Dünya!";
        var encoded = _service.Encode(original, BinaryFormat.Compact);
        Assert.True(encoded.IsSuccess);

        var decoded = _service.Decode(encoded.Value);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }
}