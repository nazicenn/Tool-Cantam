using System;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class HmacServiceTests
{
    private readonly HmacService _service = new();

    [Fact]
    public void Compute_Sha256_TextKey_ReturnsBytes()
    {
        var r = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        Assert.True(r.IsSuccess);
        Assert.Equal(32, r.Value!.Length); // SHA256 = 32 byte
    }

    [Fact]
    public void Compute_Sha384_Returns48Bytes()
    {
        var r = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha384);
        Assert.True(r.IsSuccess);
        Assert.Equal(48, r.Value!.Length);
    }

    [Fact]
    public void Compute_Sha512_Returns64Bytes()
    {
        var r = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha512);
        Assert.True(r.IsSuccess);
        Assert.Equal(64, r.Value!.Length);
    }

    [Fact]
    public void Compute_EmptyKey_ReturnsValidation()
    {
        var r = _service.Compute("hello", "", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

       [Fact]
    public void Compute_InvalidHexKey_ReturnsValidation()
    {
        var r = _service.Compute("hello", "not-hex!!!", HmacKeyFormat.Hex, HashAlgorithmKind.Sha256);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Compute_Deterministic_SameResult()
    {
        var r1 = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        var r2 = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.Equal(r1.Value, r2.Value);
    }

    [Fact]
    public void Compute_DifferentMessages_DifferentResults()
    {
        var r1 = _service.Compute("hello", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        var r2 = _service.Compute("world", "secret", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);
        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.NotEqual(r1.Value, r2.Value);
    }

    // ---------- TryParseMac ----------

    [Fact]
    public void TryParseMac_ValidHex_ReturnsTrue()
    {
        var r = _service.TryParseMac("ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789", out var mac);
        Assert.True(r);
        Assert.Equal(32, mac.Length);
    }

    [Fact]
    public void TryParseMac_HexWithSpaces_Works()
    {
        var r = _service.TryParseMac("AB CD EF 01", out var mac);
        Assert.True(r);
        Assert.Equal(4, mac.Length);
    }

    [Fact]
    public void TryParseMac_HexWithColons_Works()
    {
        var r = _service.TryParseMac("AB:CD:EF:01", out var mac);
        Assert.True(r);
        Assert.Equal(4, mac.Length);
    }

    [Fact]
    public void TryParseMac_ValidBase64_ReturnsTrue()
    {
        var r = _service.TryParseMac("q83vASNFZ4mrze8=", out var mac);
        Assert.True(r);
        Assert.True(mac.Length > 0);
    }

    [Fact]
    public void TryParseMac_Invalid_ReturnsFalse()
    {
        var r = _service.TryParseMac("!!!invalid!!!", out var mac);
        Assert.False(r);
        Assert.Empty(mac);
    }

    [Fact]
    public void TryParseMac_Empty_ReturnsFalse()
    {
        var r = _service.TryParseMac("", out var mac);
        Assert.False(r);
        Assert.Empty(mac);
    }

    [Fact]
    public void TryParseMac_Null_ReturnsFalse()
    {
        var r = _service.TryParseMac(null, out var mac);
        Assert.False(r);
        Assert.Empty(mac);
    }
}