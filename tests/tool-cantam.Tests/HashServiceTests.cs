using System.IO;
using System.Security.Cryptography;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// Hash servisinin sözleşmesini kilitler: bilinen test vektörleri, platform ilkelleriyle uyum,
/// algoritma listesinin gerçekten kullanılabilir olması, dosya akışı ve hata yolları.
/// </summary>
public class HashServiceTests
{
    private readonly HashService _service = new();

    // ---------------------------------------------------------------------
    // Bilinen test vektörleri
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(HashAlgorithmKind.Md5, "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData(HashAlgorithmKind.Sha1, "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData(HashAlgorithmKind.Sha256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData(HashAlgorithmKind.Sha384, "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7")]
    [InlineData(HashAlgorithmKind.Sha512, "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")]
    public void ComputeFromText_matches_the_published_vectors_for_abc(HashAlgorithmKind kind, string expected)
    {
        ToolResult<byte[]> result = _service.ComputeFromText("abc", kind);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, Convert.ToHexString(result.Value!).ToLowerInvariant());
    }

    [Fact]
    public void ComputeFromText_hashes_the_empty_string_instead_of_returning_nothing()
    {
        // Boş metnin özeti tanımlıdır; eskiden boş sonuç dönüyordu ve bu yanıltıcıydı.
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", Hex(HashAlgorithmKind.Md5, string.Empty));
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", Hex(HashAlgorithmKind.Sha256, string.Empty));
    }

    [Theory]
    [InlineData(HashAlgorithmKind.Sha3_256, "3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532")]
    public void ComputeFromText_matches_the_nist_sha3_vector_when_available(HashAlgorithmKind kind, string expected)
    {
        if (!IsAvailable(kind))
            return;   // bu sistemde yoksa doğrulanacak bir şey yok

        Assert.Equal(expected, Hex(kind, "abc"));
    }

    [Fact]
    public void Every_reported_algorithm_agrees_with_the_platform_primitive()
    {
        byte[] input = Encoding.UTF8.GetBytes("Türkçe içerik 😀 123");

        foreach (HashAlgorithmDescriptor descriptor in _service.Algorithms)
        {
            byte[] expected = descriptor.Kind switch
            {
                HashAlgorithmKind.Md5 => MD5.HashData(input),
                HashAlgorithmKind.Sha1 => SHA1.HashData(input),
                HashAlgorithmKind.Sha256 => SHA256.HashData(input),
                HashAlgorithmKind.Sha384 => SHA384.HashData(input),
                HashAlgorithmKind.Sha512 => SHA512.HashData(input),
                HashAlgorithmKind.Sha3_256 => SHA3_256.HashData(input),
                HashAlgorithmKind.Sha3_512 => SHA3_512.HashData(input),
                _ => throw new InvalidOperationException($"Beklenmeyen algoritma: {descriptor.Kind}")
            };

            ToolResult<byte[]> result = _service.ComputeFromText("Türkçe içerik 😀 123", descriptor.Kind);

            Assert.True(result.IsSuccess);
            Assert.Equal(expected, result.Value);
            Assert.Equal(descriptor.DigestSizeBytes, result.Value!.Length);
        }
    }

    [Fact]
    public void ComputeFromText_uses_utf8_for_non_ascii_text()
    {
        byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes("ğüşiöç"));

        ToolResult<byte[]> result = _service.ComputeFromText("ğüşiöç", HashAlgorithmKind.Sha256);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ComputeFromText_is_deterministic()
    {
        byte[] first = _service.ComputeFromText("aynı metin", HashAlgorithmKind.Sha256).Value!;
        byte[] second = _service.ComputeFromText("aynı metin", HashAlgorithmKind.Sha256).Value!;

        Assert.Equal(first, second);
    }

    // ---------------------------------------------------------------------
    // Algoritma listesi
    // ---------------------------------------------------------------------

    [Fact]
    public void Core_algorithms_are_always_available()
    {
        foreach (HashAlgorithmKind kind in new[]
                 {
                     HashAlgorithmKind.Md5, HashAlgorithmKind.Sha1, HashAlgorithmKind.Sha256,
                     HashAlgorithmKind.Sha384, HashAlgorithmKind.Sha512
                 })
        {
            Assert.True(IsAvailable(kind), $"{kind} bu sistemde bulunmalıydı.");
        }
    }

    [Fact]
    public void Weak_algorithms_are_flagged()
    {
        Assert.True(Descriptor(HashAlgorithmKind.Md5).IsWeak);
        Assert.True(Descriptor(HashAlgorithmKind.Sha1).IsWeak);
        Assert.False(Descriptor(HashAlgorithmKind.Sha256).IsWeak);
        Assert.False(Descriptor(HashAlgorithmKind.Sha512).IsWeak);
    }

    [Fact]
    public void Descriptors_report_the_correct_hex_length()
    {
        Assert.Equal(32, Descriptor(HashAlgorithmKind.Md5).HexLength);
        Assert.Equal(40, Descriptor(HashAlgorithmKind.Sha1).HexLength);
        Assert.Equal(64, Descriptor(HashAlgorithmKind.Sha256).HexLength);
        Assert.Equal(96, Descriptor(HashAlgorithmKind.Sha384).HexLength);
        Assert.Equal(128, Descriptor(HashAlgorithmKind.Sha512).HexLength);
    }

    // ---------------------------------------------------------------------
    // Dosya
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ComputeFromFile_equals_hashing_the_same_bytes()
    {
        string path = Path.Combine(Path.GetTempPath(), "tool-cantam-hash-" + Guid.NewGuid().ToString("N") + ".bin");

        try
        {
            byte[] content = Encoding.UTF8.GetBytes("Dosya içeriği 😀");
            await File.WriteAllBytesAsync(path, content);

            ToolResult<byte[]> fromFile = await _service.ComputeFromFileAsync(path, HashAlgorithmKind.Sha256);

            Assert.True(fromFile.IsSuccess);
            Assert.Equal(SHA256.HashData(content), fromFile.Value);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ComputeFromFile_can_be_cancelled()
    {
        string path = Path.Combine(Path.GetTempPath(), "tool-cantam-hash-" + Guid.NewGuid().ToString("N") + ".bin");

        try
        {
            await File.WriteAllBytesAsync(path, new byte[1024 * 1024]);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            ToolResult<byte[]> result = await _service.ComputeFromFileAsync(path, HashAlgorithmKind.Sha256, cancellation.Token);

            Assert.False(result.IsSuccess);
            Assert.Equal(ToolErrorKind.Cancelled, result.ErrorKind);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ComputeFromFile_reports_a_missing_file()
    {
        string path = Path.Combine(Path.GetTempPath(), "tool-cantam-missing-" + Guid.NewGuid().ToString("N") + ".bin");

        ToolResult<byte[]> result = await _service.ComputeFromFileAsync(path, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, result.ErrorKind);
        Assert.Equal(HashErrorCodes.FileNotFound, result.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ComputeFromFile_requires_a_path(string path)
    {
        ToolResult<byte[]> result = await _service.ComputeFromFileAsync(path, HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
        Assert.Equal(HashErrorCodes.PathRequired, result.ErrorCode);
    }

    [Fact]
    public void ComputeFromText_reports_too_large_input()
    {
        ToolResult<byte[]> result = _service.ComputeFromText(new string('a', HashService.MaxTextLengthLimit + 1), HashAlgorithmKind.Sha256);

        Assert.False(result.IsSuccess);
        Assert.Equal(HashErrorCodes.InputTooLarge, result.ErrorCode);
    }

    [Fact]
    public void ComputeFromText_reports_an_unknown_algorithm()
    {
        ToolResult<byte[]> result = _service.ComputeFromText("abc", (HashAlgorithmKind)999);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
        Assert.Equal(HashErrorCodes.UnsupportedAlgorithm, result.ErrorCode);
    }

    private string Hex(HashAlgorithmKind kind, string text)
    {
        ToolResult<byte[]> result = _service.ComputeFromText(text, kind);
        Assert.True(result.IsSuccess);
        return Convert.ToHexString(result.Value!).ToLowerInvariant();
    }

    private bool IsAvailable(HashAlgorithmKind kind)
    {
        foreach (HashAlgorithmDescriptor descriptor in _service.Algorithms)
        {
            if (descriptor.Kind == kind)
                return true;
        }

        return false;
    }

    private HashAlgorithmDescriptor Descriptor(HashAlgorithmKind kind)
    {
        foreach (HashAlgorithmDescriptor descriptor in _service.Algorithms)
        {
            if (descriptor.Kind == kind)
                return descriptor;
        }

        throw new InvalidOperationException($"{kind} bu sistemde bulunamadı.");
    }
}
