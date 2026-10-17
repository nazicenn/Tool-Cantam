using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hash tanımlayıcının sözleşmesini kilitler: uzunluk/karakter kümesi tabanlı adaylar, ayırt edici
/// önekler, güven düzeyleri ve eski sürümün NTLM yanlış-pozitifinin geri dönmemesi.
/// </summary>
public class HashIdentifierServiceTests
{
    private readonly HashIdentifierService _service = new();

    // ---------------------------------------------------------------------
    // Uzunluğa göre adaylar
    // ---------------------------------------------------------------------

    [Fact]
    public void Thirty_two_hex_characters_are_reported_as_several_possible_algorithms()
    {
        HashIdentificationResult result = Identify(new string('a', 32));

        Assert.Equal(HashCharacterSet.Hex, result.CharacterSet);
        Assert.Equal(32, result.Length);
        Assert.True(result.IsAmbiguous);
        Assert.Equal(HashIdentifierConfidence.Possible, result.Candidates[0].Confidence);

        string[] algorithms = result.Candidates.Select(candidate => candidate.Algorithm).ToArray();
        Assert.Contains("MD5", algorithms);
        Assert.Contains("NTLM", algorithms);
        Assert.Contains("MD4", algorithms);
    }

    [Fact]
    public void Thirty_two_alphanumeric_characters_are_not_called_ntlm()
    {
        // Eski sürüm, hex olmayan 32 karakterlik alfanümerik girdiyi "NTLM" sayıyordu.
        // NTLM hex'tir; bu yüzden bu girdi hiçbir algoritmaya uymamalı.
        HashIdentificationResult result = Identify(new string('z', 32));

        Assert.Equal(HashCharacterSet.Alphanumeric, result.CharacterSet);
        Assert.False(result.IsIdentified);
        Assert.Empty(result.Candidates);
    }

    [Theory]
    [InlineData(8, "CRC-32")]
    [InlineData(56, "SHA-224")]
    public void Shapes_with_a_single_candidate_are_reported_as_likely(int length, string expectedAlgorithm)
    {
        HashIdentificationResult result = Identify(new string('a', length));

        HashCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(expectedAlgorithm, candidate.Algorithm);
        Assert.Equal(HashIdentifierConfidence.Likely, candidate.Confidence);
        Assert.False(result.IsAmbiguous);
    }

    [Theory]
    [InlineData(40, new[] { "SHA-1", "RIPEMD-160", "MySQL5" })]
    [InlineData(64, new[] { "SHA-256", "SHA3-256", "BLAKE2s-256" })]
    [InlineData(96, new[] { "SHA-384", "SHA3-384" })]
    [InlineData(128, new[] { "SHA-512", "SHA3-512", "BLAKE2b-512", "Whirlpool" })]
    public void Longer_hex_shapes_list_every_algorithm_that_fits(int length, string[] expectedAlgorithms)
    {
        HashIdentificationResult result = Identify(new string('a', length));

        string[] algorithms = result.Candidates.Select(candidate => candidate.Algorithm).ToArray();

        foreach (string expected in expectedAlgorithms)
            Assert.Contains(expected, algorithms);
    }

    // ---------------------------------------------------------------------
    // Ayırt edici önekler
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("$2b$12$abcdefghijklmnopqrstuv", "bcrypt")]
    [InlineData("$2y$10$abcdefghijklmnopqrstuv", "bcrypt")]
    [InlineData("$argon2id$v=19$m=65536,t=3,p=4$c29tZXNhbHQ$abc", "Argon2id")]
    [InlineData("$argon2i$v=19$m=4096,t=3,p=1$c29tZXNhbHQ$abc", "Argon2i")]
    [InlineData("$1$abcdefgh$abcdefghijklmnopqrstuv", "MD5 crypt")]
    [InlineData("$6$abcdefgh$abcdefghijklmnopqrstuv", "SHA-512 crypt")]
    [InlineData("$5$abcdefgh$abcdefghijklmnopqrstuv", "SHA-256 crypt")]
    [InlineData("$apr1$abcdefgh$abcdefghijklmnopqrstuv", "Apache MD5 crypt")]
    [InlineData("{SSHA}abcdefghijklmnopqrstuv", "LDAP SSHA")]
    [InlineData("{SHA}abcdefghijklmnopqrstuv", "LDAP SHA")]
    public void Distinctive_prefixes_produce_a_certain_result(string value, string expectedAlgorithm)
    {
        HashIdentificationResult result = Identify(value);

        HashCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal(expectedAlgorithm, candidate.Algorithm);
        Assert.Equal(HashIdentifierConfidence.Certain, candidate.Confidence);
        Assert.True(result.HasDistinctivePrefix);
    }

    [Fact]
    public void MySql_native_password_shape_is_recognised()
    {
        HashIdentificationResult result = Identify("*" + new string('A', 40));

        HashCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal("MySQL native password", candidate.Algorithm);
        Assert.Equal(HashIdentifierConfidence.Likely, candidate.Confidence);
    }

    // ---------------------------------------------------------------------
    // Base64, crypt ve 0x
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(24, new[] { "MD5", "NTLM" })]
    [InlineData(28, new[] { "SHA-1", "RIPEMD-160" })]
    [InlineData(44, new[] { "SHA-256", "SHA3-256" })]
    [InlineData(88, new[] { "SHA-512", "SHA3-512" })]
    public void Base64_shapes_are_recognised(int length, string[] expectedAlgorithms)
    {
        // 'G' hex basamağı değildir; dolgu işareti ise Base64 olduğunu kesinleştirir.
        string value = new string('G', length - 2) + "==";

        HashIdentificationResult result = Identify(value);

        Assert.Equal(HashCharacterSet.Base64, result.CharacterSet);

        string[] algorithms = result.Candidates.Select(candidate => candidate.Algorithm).ToArray();
        foreach (string expected in expectedAlgorithms)
            Assert.Contains(expected, algorithms);
    }

    [Fact]
    public void Pure_alphanumeric_values_are_labelled_alphanumeric_but_are_still_checked_as_base64()
    {
        // Dolgu/özel karakter yoksa "alfanümerik" denir; yine de Base64 özet uzunluğundaysa adaylar çıkar.
        HashIdentificationResult result = Identify(new string('G', 44));

        Assert.Equal(HashCharacterSet.Alphanumeric, result.CharacterSet);
        Assert.Contains("SHA-256", result.Candidates.Select(candidate => candidate.Algorithm));
    }

    [Fact]
    public void Des_crypt_shape_is_recognised()
    {
        HashIdentificationResult result = Identify("abJnggxhB/yWI");

        Assert.Equal(HashCharacterSet.DesCrypt, result.CharacterSet);
        HashCandidate candidate = Assert.Single(result.Candidates);
        Assert.Equal("DES crypt", candidate.Algorithm);
    }

    [Fact]
    public void Zero_x_prefix_is_stripped_before_the_length_is_measured()
    {
        HashIdentificationResult result = Identify("0x" + new string('a', 64));

        Assert.Equal(64, result.Length);
        string[] algorithms = result.Candidates.Select(candidate => candidate.Algorithm).ToArray();
        Assert.Contains("SHA-256", algorithms);
    }

    // ---------------------------------------------------------------------
    // Tanımlanamayan ve hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("hello world!")]
    [InlineData("not-a-hash")]
    [InlineData("AAAA")]
    public void Unrecognised_shapes_return_no_candidates(string value)
    {
        HashIdentificationResult result = Identify(value);

        Assert.False(result.IsIdentified);
        Assert.Empty(result.Candidates);
        Assert.False(result.IsAmbiguous);
    }

    [Fact]
    public void Whitespace_is_trimmed_before_identification()
    {
        HashIdentificationResult result = Identify("   " + new string('a', 64) + "   ");

        Assert.Equal(64, result.Length);
        Assert.True(result.IsIdentified);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_input_is_rejected(string value)
    {
        ToolResult<HashIdentificationResult> result = _service.Identify(value);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
        Assert.Equal(HashIdentifierErrorCodes.EmptyInput, result.ErrorCode);
    }

    [Fact]
    public void Overlong_input_is_rejected()
    {
        ToolResult<HashIdentificationResult> result = _service.Identify(new string('a', HashIdentifierService.MaxInputLengthLimit + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(HashIdentifierErrorCodes.InputTooLong, result.ErrorCode);
    }

    private HashIdentificationResult Identify(string value)
    {
        ToolResult<HashIdentificationResult> result = _service.Identify(value);
        Assert.True(result.IsSuccess, $"{result.ErrorKind}/{result.ErrorCode}: {result.TechnicalDetail}");
        return result.Value!;
    }
}
