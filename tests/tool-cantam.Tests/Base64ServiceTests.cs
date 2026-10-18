using tool_cantam.Services.ErrorCodes;
using System.IO;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Base64 servisinin sözleşmesini kilitler: RFC 4648 vektörleri, alfabe katılığı,
/// boşluk/dolgu toleransı, boyut sınırları ve dosya akışı davranışı.
/// </summary>
public class Base64ServiceTests
{
    private readonly Base64Service _service = new();

    // ---------------------------------------------------------------------
    // Metin kodlama: RFC 4648 §10 test vektörleri
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg==")]
    [InlineData("fo", "Zm8=")]
    [InlineData("foo", "Zm9v")]
    [InlineData("foob", "Zm9vYg==")]
    [InlineData("fooba", "Zm9vYmE=")]
    [InlineData("foobar", "Zm9vYmFy")]
    public void EncodeText_matches_rfc4648_vectors(string plainText, string expected)
    {
        ToolResult<string> result = _service.EncodeText(plainText);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Zg==", "f")]
    [InlineData("Zm8=", "fo")]
    [InlineData("Zm9v", "foo")]
    [InlineData("Zm9vYg==", "foob")]
    [InlineData("Zm9vYmE=", "fooba")]
    [InlineData("Zm9vYmFy", "foobar")]
    public void DecodeText_matches_rfc4648_vectors(string base64Text, string expected)
    {
        ToolResult<string> result = _service.DecodeText(base64Text);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void Turkish_characters_survive_a_round_trip()
    {
        const string original = "İstanbul ığüşöçıI ĞÜŞÖÇ";

        ToolResult<string> encoded = _service.EncodeText(original);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.DecodeText(encoded.Value!);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }

    // ---------------------------------------------------------------------
    // Girdi toleransı
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Zm9v\nYmFy")]          // satır sonu
    [InlineData("Zm9v YmFy")]           // boşluk
    [InlineData("Zm9v\r\nYmFy")]        // CRLF
    [InlineData("  Zm9vYmFy  ")]        // baş/son boşluk
    [InlineData("Zm9v\tYmFy")]          // sekme
    public void DecodeText_ignores_whitespace(string input)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.True(result.IsSuccess);
        Assert.Equal("foobar", result.Value);
    }

    [Theory]
    [InlineData("Zm9vYg")]              // eksik dolgu
    [InlineData("Zm9vYg=")]             // eksik dolgu (tek)
    [InlineData("Zm8")]                 // eksik dolgu
    [InlineData("Zg")]                  // çok kısa
    public void DecodeText_accepts_missing_padding(string input)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t\n")]
    public void DecodeText_returns_empty_for_blank_input(string input)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public void EncodeText_returns_empty_for_empty_input()
    {
        ToolResult<string> result = _service.EncodeText(string.Empty);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    // ---------------------------------------------------------------------
    // Alfabe (RFC 4648 §4 ve §5)
    // ---------------------------------------------------------------------

    [Fact]
    public void UrlSafe_alphabet_replaces_plus_and_slash_and_omits_padding()
    {
        // U+FFFF + 'a' girdisinin UTF-8 baytları (EF BF BF 61) standart alfabede "77+/YQ==" üretir.
        const string input = "\uFFFFa";

        ToolResult<string> standard = _service.EncodeText(input);
        ToolResult<string> urlSafe = _service.EncodeText(input, Base64Variant.UrlSafe);

        Assert.True(standard.IsSuccess);
        Assert.True(urlSafe.IsSuccess);

        Assert.Equal("77+/YQ==", standard.Value);
        Assert.Equal("77-_YQ", urlSafe.Value);
        Assert.DoesNotContain('+', urlSafe.Value!);
        Assert.DoesNotContain('/', urlSafe.Value!);
        Assert.DoesNotContain('=', urlSafe.Value!);
    }

    [Fact]
    public void UrlSafe_input_round_trips()
    {
        const string original = "ünicode içerik ?&=/+ test";

        ToolResult<string> encoded = _service.EncodeText(original, Base64Variant.UrlSafe);
        Assert.True(encoded.IsSuccess);

        ToolResult<string> decoded = _service.DecodeText(encoded.Value!, Base64Variant.UrlSafe);
        Assert.True(decoded.IsSuccess);
        Assert.Equal(original, decoded.Value);
    }

    [Fact]
    public void DecodeText_accepts_standard_alphabet_when_standard_variant_is_requested()
    {
        // "+/8=" geçerli Base64'tür ama çözülen baytlar UTF-8 değildir; bu yüzden
        // geçerli UTF-8 üreten "+" ve "/" içeren bir vektör kullanılır.
        ToolResult<string> result = _service.DecodeText("77+/YQ==", Base64Variant.Standard);

        Assert.True(result.IsSuccess);
        Assert.Equal("\uFFFFa", result.Value);
    }

    [Theory]
    [InlineData("+/8=")]
    [InlineData("ab+c")]
    public void DecodeText_rejects_standard_characters_in_url_safe_mode(string input)
    {
        ToolResult<string> result = _service.DecodeText(input, Base64Variant.UrlSafe);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    [Theory]
    [InlineData("-_8")]
    [InlineData("ab-c")]
    public void DecodeText_rejects_url_safe_characters_in_standard_mode(string input)
    {
        ToolResult<string> result = _service.DecodeText(input, Base64Variant.Standard);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    // ---------------------------------------------------------------------
    // Hatalı girdi
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Zm9v!", Base64ErrorCodes.InvalidCharacter)]
    [InlineData("Zm9v*", Base64ErrorCodes.InvalidCharacter)]
    [InlineData("Zm9v?", Base64ErrorCodes.InvalidCharacter)]
    [InlineData("ab@cd", Base64ErrorCodes.InvalidCharacter)]
    public void DecodeText_reports_invalid_characters(string input, string expectedCode)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedCode, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
        Assert.NotNull(result.TechnicalDetail);
    }

    [Theory]
    [InlineData("Zm9vY")]
    [InlineData("a")]
    [InlineData("abcde")]
    public void DecodeText_reports_invalid_payload_length(string input)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(Base64ErrorCodes.InvalidLength, result.ErrorCode);
    }

    [Theory]
    [InlineData("Zg===")]
    [InlineData("Z=g=")]
    [InlineData("====")]
    public void DecodeText_reports_invalid_padding(string input)
    {
        ToolResult<string> result = _service.DecodeText(input);

        Assert.False(result.IsSuccess);
        Assert.Equal(Base64ErrorCodes.InvalidPadding, result.ErrorCode);
    }

    [Fact]
    public void DecodeText_reports_binary_content_instead_of_returning_broken_text()
    {
        // 0xFF 0xFE geçerli UTF-8 değildir.
        string base64 = Convert.ToBase64String(new byte[] { 0xFF, 0xFE });

        ToolResult<string> result = _service.DecodeText(base64);

        Assert.False(result.IsSuccess);
        Assert.Equal(Base64ErrorCodes.NotUtf8Text, result.ErrorCode);
        Assert.Equal(ToolErrorKind.InvalidFormat, result.ErrorKind);
    }

    [Fact]
    public void DecodeText_reports_too_large_result_before_allocating()
    {
        // Beklenen çözülmüş boyut sınırı aşacak kadar büyük bir Base64 metni.
        string oversized = new string('A', ((Base64Service.MaxTextBytesLimit / 3) + 8) * 4);

        ToolResult<string> result = _service.DecodeText(oversized);

        Assert.False(result.IsSuccess);
        Assert.Equal(Base64ErrorCodes.OutputTooLarge, result.ErrorCode);
        Assert.Equal(ToolErrorKind.TooLarge, result.ErrorKind);
    }

    // ---------------------------------------------------------------------
    // Dosya işlemleri
    // ---------------------------------------------------------------------

    [Fact]
    public async Task File_encode_decode_round_trip_preserves_bytes()
    {
        string directory = CreateTempDirectory();
        try
        {
            byte[] original = new byte[4096];
            Random.Shared.NextBytes(original);

            string source = Path.Combine(directory, "source.bin");
            string encoded = Path.Combine(directory, "encoded.b64");
            string decoded = Path.Combine(directory, "decoded.bin");
            await File.WriteAllBytesAsync(source, original, TestContext.Current.CancellationToken);

            ToolResult<long> encodeResult = await _service.EncodeFileAsync(source, encoded, TestContext.Current.CancellationToken);
            Assert.True(encodeResult.IsSuccess, $"{encodeResult.ErrorKind}/{encodeResult.ErrorCode}: {encodeResult.TechnicalDetail}");
            Assert.True(File.Exists(encoded));
            Assert.True(encodeResult.Value > 0);

            ToolResult<long> decodeResult = await _service.DecodeFileAsync(encoded, decoded, TestContext.Current.CancellationToken);
            Assert.True(decodeResult.IsSuccess, $"{decodeResult.ErrorKind}/{decodeResult.ErrorCode}: {decodeResult.TechnicalDetail}");

            Assert.Equal(original, await File.ReadAllBytesAsync(decoded, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task File_decode_ignores_line_breaks_written_by_other_tools()
    {
        string directory = CreateTempDirectory();
        try
        {
            // 76 karakterde satır sonu ekleyen klasik biçim (PEM/e-posta gövdeleri).
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(new string('x', 300)));
            var wrapped = new StringBuilder();
            for (int index = 0; index < base64.Length; index += 76)
                wrapped.AppendLine(base64.Substring(index, Math.Min(76, base64.Length - index)));

            string source = Path.Combine(directory, "wrapped.b64");
            string decoded = Path.Combine(directory, "decoded.txt");
            await File.WriteAllTextAsync(source, wrapped.ToString(), TestContext.Current.CancellationToken);

            ToolResult<long> result = await _service.DecodeFileAsync(source, decoded, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess);
            Assert.Equal(new string('x', 300), await File.ReadAllTextAsync(decoded, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task File_decode_reports_invalid_content_and_leaves_no_partial_output()
    {
        string directory = CreateTempDirectory();
        try
        {
            string source = Path.Combine(directory, "broken.b64");
            string destination = Path.Combine(directory, "should-not-exist.bin");
            await File.WriteAllTextAsync(source, "this is definitely not base64 ###", TestContext.Current.CancellationToken);

            ToolResult<long> result = await _service.DecodeFileAsync(source, destination, TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(Base64ErrorCodes.InvalidFileContent, result.ErrorCode);

            // Yarım çıktı ve geçici dosya kalmamalı.
            Assert.False(File.Exists(destination));
            Assert.False(File.Exists(destination + ".tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task File_encode_reports_missing_source_file()
    {
        string directory = CreateTempDirectory();
        try
        {
            string missing = Path.Combine(directory, "missing.bin");
            string destination = Path.Combine(directory, "out.b64");

            ToolResult<long> result = await _service.EncodeFileAsync(missing, destination, TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(ToolErrorKind.NotFound, result.ErrorKind);
            Assert.Equal(Base64ErrorCodes.FileNotFound, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("", "out.b64")]
    [InlineData("source.bin", "")]
    public async Task File_operations_require_both_paths(string sourceName, string destinationName)
    {
        ToolResult<long> result = await _service.EncodeFileAsync(sourceName, destinationName, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
        Assert.Equal(Base64ErrorCodes.PathRequired, result.ErrorCode);
    }

    [Fact]
    public async Task File_encode_is_cancellable_and_leaves_no_temp_file()
    {
        string directory = CreateTempDirectory();
        try
        {
            string source = Path.Combine(directory, "large.bin");
            string destination = Path.Combine(directory, "large.b64");

            // İptal işlem başlamadan önce isteniyor; dosya boyutu yalnızca akışın kurulmasını sağlar.
            await File.WriteAllBytesAsync(source, new byte[2 * 1024 * 1024], TestContext.Current.CancellationToken);

            // Test çalıştırması iptal edilirse bu test de hızla bitsin.
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cancellation.Cancel();

            ToolResult<long> result = await _service.EncodeFileAsync(source, destination, cancellation.Token);

            Assert.False(result.IsSuccess);
            Assert.Equal(ToolErrorKind.Cancelled, result.ErrorKind);
            Assert.False(File.Exists(destination));
            Assert.False(File.Exists(destination + ".tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "tool-cantam-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
