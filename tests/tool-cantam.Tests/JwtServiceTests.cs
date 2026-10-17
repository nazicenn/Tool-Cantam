using System;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// JWT decode servisinin sözleşmesini kilitler: geçerli token, standart claim'ler,
/// süre dolumu, geçersiz format ve hata yolları.
/// </summary>
public class JwtServiceTests
{
    private readonly JwtService _service = new();

    // Hazır test token'ları (base64url, imzasız)
    // {"alg":"HS256","typ":"JWT"} . {"sub":"1234","name":"test","iat":1516239022} .
    private const string SimpleToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
        "eyJzdWIiOiIxMjM0IiwibmFtZSI6InRlc3QiLCJpYXQiOjE1MTYyMzkwMjJ9." +
        "signature";

    // exp geçmişte olan token
    private const string ExpiredToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
        "eyJzdWIiOiIxMjM0IiwiZXhwIjoxNTE2MjM5MDIyfQ." +
        "signature";

    // ---------------------------------------------------------------------
    // Geçerli token
    // ---------------------------------------------------------------------

    [Fact]
    public void Decode_extracts_header_and_payload()
    {
        var r = _service.Decode(SimpleToken);
        Assert.True(r.IsSuccess);

        var report = r.Value!;
        Assert.Equal("HS256", report.Algorithm);
        Assert.Equal("JWT", report.TokenType);
        Assert.Equal("1234", report.Subject);
        Assert.Contains("HS256", report.Header);
        Assert.Contains("\"name\"", report.Payload);
    }

    [Fact]
    public void Decode_parses_issued_at_claim()
    {
        var r = _service.Decode(SimpleToken);
        Assert.True(r.IsSuccess);
        Assert.NotNull(r.Value!.IssuedAt);
        Assert.Equal(2018, r.Value.IssuedAt!.Value.Year);
    }

    [Fact]
    public void Decode_detects_expired_token()
    {
        var r = _service.Decode(ExpiredToken);
        Assert.True(r.IsSuccess);

        var report = r.Value!;
        Assert.NotNull(report.Expiration);
        Assert.True(report.IsExpired);
        Assert.NotNull(report.TimeUntilExpiry);
        Assert.True(report.TimeUntilExpiry.Value.TotalSeconds < 0);
    }

    [Fact]
    public void Decode_accepts_token_without_signature()
    {
        // 2 parçalı token (signature yok)
        string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0";

        var r = _service.Decode(token);
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Value!.Signature);
    }

    // ---------------------------------------------------------------------
    // Hata yolları
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_input_is_rejected(string? input)
    {
        var r = _service.Decode(input);
        Assert.False(r.IsSuccess);
        Assert.Equal(JwtErrorCodes.EmptyInput, r.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("onlyone")]
    [InlineData("a.b.c.d")]
    public void Invalid_format_is_rejected(string input)
    {
        var r = _service.Decode(input);
        Assert.False(r.IsSuccess);
        Assert.True(
            r.ErrorCode == JwtErrorCodes.InvalidFormat ||
            r.ErrorCode == JwtErrorCodes.InvalidBase64);
    }

    [Fact]
    public void Invalid_base64_segment_is_rejected()
    {
        string token = "!!!invalid!!!.eyJzdWIiOiIxMjM0In0.sig";

        var r = _service.Decode(token);
        Assert.False(r.IsSuccess);
        Assert.Equal(JwtErrorCodes.InvalidBase64, r.ErrorCode);
    }

    [Fact]
    public void Invalid_json_segment_is_rejected()
    {
        // base64url("not-json") = "bm90LWpzb24"
        string token = "bm90LWpzb24.eyJzdWIiOiIxMjM0In0.sig";

        var r = _service.Decode(token);
        Assert.False(r.IsSuccess);
        Assert.Equal(JwtErrorCodes.InvalidJson, r.ErrorCode);
    }

    [Fact]
    public void Too_large_token_is_reported()
    {
        string huge = new string('a', JwtService.MaxTokenLengthLimit + 1);
        var r = _service.Decode(huge);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
        Assert.Equal(JwtErrorCodes.TokenTooLarge, r.ErrorCode);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.MaxTokenLength > 0);
        Assert.True(_service.MaxTokenLength <= 1024 * 1024);
    }
}