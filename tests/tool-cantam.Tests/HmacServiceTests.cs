using System.Security.Cryptography;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// HMAC servisinin sözleşmesini kilitler: RFC 4231 test vektörleri, hex/metin anahtar biçimi,
/// anahtar doğrulaması, MAC çözümleme ve hata yolları.
/// </summary>
public class HmacServiceTests
{
    private readonly HmacService _service = new();

    // ---------------------------------------------------------------------
    // RFC 4231 test vektörleri
    // ---------------------------------------------------------------------

    [Fact]
    public void HmacSha256_matches_rfc4231_test_case_1()
    {
        // key = 0x0b repeated 20 times, data = "Hi There"
        string key = string.Concat(Enumerable.Repeat("0b", 20));

        ToolResult<byte[]> result = _service.Compute("Hi There", key, HmacKeyFormat.Hex, HashAlgorithmKind.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7",
            Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void HmacSha256_matches_rfc4231_test_case_2()
    {
        ToolResult<byte[]> result = _service.Compute(
            "what do ya want for nothing?", "Jefe", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843",
            Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void HmacSha512_matches_rfc4231_test_case_2()
    {
        ToolResult<byte[]> result = _service.Compute(
            "what do ya want for nothing?", "Jefe", HmacKeyFormat.Text, HashAlgorithmKind.Sha512);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "164b7a7bfcf819e2e395fbe73b56e0a387bd64222e831fd610270cd7ea2505549758bf75c05a994a6d034f65f8f0e6fdcaeab1a34d4a6b4b636e070a38bce737",
            Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void HmacSha1_matches_rfc2202_test_case_1()
    {
        string key = string.Concat(Enumerable.Repeat("0b", 20));

        ToolResult<byte[]> result = _service.Compute("Hi There", key, HmacKeyFormat.Hex, HashAlgorithmKind.Sha1);

        Assert.True(result.IsSuccess);
        Assert.Equal("b617318655057264e28bc0b6fb378c8ef146be00", Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void HmacMd5_matches_rfc2104_test_case_2()
    {
        ToolResult<byte[]> result = _service.Compute(
            "what do ya want for nothing?", "Jefe", HmacKeyFormat.Text, HashAlgorithmKind.Md5);

        Assert.True(result.IsSuccess);
        Assert.Equal("750c783e6ab0b503eaa86e310a5db738", Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void Every_reported_algorithm_agrees_with_the_platform_primitive()
    {
        byte[] key = Encoding.UTF8.GetBytes("gizli anahtar");
        byte[] message = Encoding.UTF8.GetBytes("Türkçe mesaj 😀");

        foreach (HashAlgorithmDescriptor descriptor in _service.Algorithms)
        {
            byte[] expected = descriptor.Kind switch
            {
                HashAlgorithmKind.Md5 => new HMACMD5(key).ComputeHash(message),
                HashAlgorithmKind.Sha1 => new HMACSHA1(key).ComputeHash(message),
                HashAlgorithmKind.Sha256 => new HMACSHA256(key).ComputeHash(message),
                HashAlgorithmKind.Sha384 => new HMACSHA384(key).ComputeHash(message),
                HashAlgorithmKind.Sha512 => new HMACSHA512(key).ComputeHash(message),
                HashAlgorithmKind.Sha3_256 => new HMACSHA3_256(key).ComputeHash(message),
                HashAlgorithmKind.Sha3_512 => new HMACSHA3_512(key).ComputeHash(message),
                _ => throw new InvalidOperationException($"Beklenmeyen algoritma: {descriptor.Kind}")
            };

            ToolResult<byte[]> actual = _service.Compute("Türkçe mesaj 😀", "gizli anahtar", HmacKeyFormat.Text, descriptor.Kind);

            Assert.True(actual.IsSuccess);
            Assert.Equal(expected, actual.Value);
            Assert.Equal(descriptor.DigestSizeBytes, actual.Value!.Length);
        }
    }

    // ---------------------------------------------------------------------
    // Anahtar biçimi
    // ---------------------------------------------------------------------

    [Fact]
    public void Text_and_hex_keys_produce_different_macs_for_the_same_characters()
    {
        string key = "0102030405";

        byte[] asText = _service.Compute("mesaj", key, HmacKeyFormat.Text, HashAlgorithmKind.Sha256).Value!;
        byte[] asHex = _service.Compute("mesaj", key, HmacKeyFormat.Hex, HashAlgorithmKind.Sha256).Value!;

        Assert.NotEqual(asText, asHex);
    }

    [Fact]
    public void Hex_keys_ignore_spaces_and_colons()
    {
        byte[] compact = _service.Compute("mesaj", "0a0b0c", HmacKeyFormat.Hex, HashAlgorithmKind.Sha256).Value!;
        byte[] spaced = _service.Compute("mesaj", "0a:0b:0c", HmacKeyFormat.Hex, HashAlgorithmKind.Sha256).Value!;

        Assert.Equal(compact, spaced);
    }

    [Theory]
    [InlineData("abc")]        // tek sayıda basamak
    [InlineData("0a0z")]       // hex olmayan karakter
    [InlineData("   ")]        // yalnızca boşluk
    public void Invalid_hex_keys_are_rejected(string key)
    {
        ToolResult<byte[]> result = _service.Compute("mesaj", key, HmacKeyFormat.Hex, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(HmacErrorCodes.InvalidHexKey, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public void Empty_key_is_rejected()
    {
        ToolResult<byte[]> result = _service.Compute("mesaj", string.Empty, HmacKeyFormat.Text, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(HmacErrorCodes.EmptyKey, result.ErrorCode);
    }

    [Fact]
    public void Overlong_key_is_rejected()
    {
        string key = new('A', (HmacService.MaxKeyBytesLimit * 2) + 2);   // hex: limitin üstünde bayt

        ToolResult<byte[]> result = _service.Compute("mesaj", key, HmacKeyFormat.Hex, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(HmacErrorCodes.KeyTooLong, result.ErrorCode);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
    }

    [Fact]
    public void Overlong_message_is_rejected()
    {
        ToolResult<byte[]> result = _service.Compute(
            new string('m', HmacService.MaxTextLengthLimit + 1), "key", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(HmacErrorCodes.InputTooLarge, result.ErrorCode);
    }

    [Fact]
    public void Unknown_algorithm_is_rejected()
    {
        ToolResult<byte[]> result = _service.Compute("mesaj", "key", HmacKeyFormat.Text, (HashAlgorithmKind)999);

        Assert.False(result.IsSuccess);
        Assert.Equal(HmacErrorCodes.UnsupportedAlgorithm, result.ErrorCode);
    }

    [Fact]
    public void Empty_message_is_allowed_because_its_mac_is_defined()
    {
        ToolResult<byte[]> result = _service.Compute(string.Empty, "key", HmacKeyFormat.Text, HashAlgorithmKind.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(32, result.Value!.Length);
    }

    // ---------------------------------------------------------------------
    // MAC çözümleme (doğrulama için)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("0A1B2C")]
    [InlineData("0a1b2c")]
    [InlineData("0A:1B:2C")]
    [InlineData("0A 1B 2C")]
    public void Hex_macs_are_parsed_regardless_of_case_and_separators(string value)
    {
        Assert.True(_service.TryParseMac(value, out byte[] mac));
        Assert.Equal(new byte[] { 0x0A, 0x1B, 0x2C }, mac);
    }

    [Fact]
    public void Base64_macs_are_parsed()
    {
        Assert.True(_service.TryParseMac("Chss", out byte[] mac));
        Assert.Equal(new byte[] { 0x0A, 0x1B, 0x2C }, mac);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zzz!")]
    [InlineData("ABC")]     // tek sayıda hex basamağı, Base64 de değil
    public void Invalid_macs_are_rejected(string? value)
    {
        Assert.False(_service.TryParseMac(value, out _));
    }

    [Fact]
    public void Core_algorithms_are_available()
    {
        foreach (HashAlgorithmKind kind in new[]
                 {
                     HashAlgorithmKind.Md5, HashAlgorithmKind.Sha1, HashAlgorithmKind.Sha256,
                     HashAlgorithmKind.Sha384, HashAlgorithmKind.Sha512
                 })
        {
            Assert.Contains(_service.Algorithms, descriptor => descriptor.Kind == kind);
        }
    }

    [Fact]
    public void Weak_variants_are_flagged()
    {
        Assert.True(_service.Algorithms.Single(d => d.Kind == HashAlgorithmKind.Md5).IsWeak);
        Assert.True(_service.Algorithms.Single(d => d.Kind == HashAlgorithmKind.Sha1).IsWeak);
        Assert.False(_service.Algorithms.Single(d => d.Kind == HashAlgorithmKind.Sha256).IsWeak);
    }
}
