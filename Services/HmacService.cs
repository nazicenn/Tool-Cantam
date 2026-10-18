using tool_cantam.Services.ErrorCodes;
using System.Security.Cryptography;
using System.Text;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Core.Text;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// HMAC hesaplama servisinin tek uygulaması.
/// <para>
/// Algoritma listesi bu makinede gerçekten çalışan HMAC varyantlarıyla sınırlıdır; MD5 ve SHA-1
/// tabanlı HMAC zayıf olarak işaretlenir. Anahtar baytları ve mesaj tamponu iş bitiminde
/// <see cref="CryptographicOperations.ZeroMemory"/> ile sıfırlanır; anahtar hiçbir zaman loglanmaz.
/// </para>
/// </summary>
public sealed class HmacService : IHmacService
{
    public const int MaxTextLengthLimit = 10 * 1024 * 1024;
    public const int MaxKeyBytesLimit = 4096;

    private static readonly HashAlgorithmDescriptor[] Candidates =
    {
        new(HashAlgorithmKind.Md5, "HMAC-MD5", "MD5", 16, IsWeak: true),
        new(HashAlgorithmKind.Sha1, "HMAC-SHA1", "SHA1", 20, IsWeak: true),
        new(HashAlgorithmKind.Sha256, "HMAC-SHA256", "SHA256", 32, IsWeak: false),
        new(HashAlgorithmKind.Sha384, "HMAC-SHA384", "SHA384", 48, IsWeak: false),
        new(HashAlgorithmKind.Sha512, "HMAC-SHA512", "SHA512", 64, IsWeak: false),
        new(HashAlgorithmKind.Sha3_256, "HMAC-SHA3-256", "SHA3-256", 32, IsWeak: false),
        new(HashAlgorithmKind.Sha3_512, "HMAC-SHA3-512", "SHA3-512", 64, IsWeak: false)
    };

    private static readonly Lazy<HashAlgorithmDescriptor[]> AvailableAlgorithms = new(Probe, isThreadSafe: true);

    public int MaxTextLength => MaxTextLengthLimit;

    public int MaxKeyBytes => MaxKeyBytesLimit;

    public IReadOnlyList<HashAlgorithmDescriptor> Algorithms => AvailableAlgorithms.Value;

    public ToolResult<byte[]> Compute(string? message, string? key, HmacKeyFormat keyFormat, HashAlgorithmKind kind)
    {
        if (!TryResolve(kind, out HashAlgorithmDescriptor? descriptor, out ToolResult<byte[]>? failure))
            return failure!;

        if (string.IsNullOrEmpty(key))
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Validation, HmacErrorCodes.EmptyKey, "The key is empty.");
        }

        if (!TryGetKeyBytes(key, keyFormat, out byte[] keyBytes, out failure))
            return failure!;

        if (keyBytes.Length > MaxKeyBytesLimit)
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            return ToolResult<byte[]>.Fail(
                ToolErrorKind.TooLarge,
                HmacErrorCodes.KeyTooLong,
                $"Key length {keyBytes.Length} bytes exceeds {MaxKeyBytesLimit}.");
        }

        string text = message ?? string.Empty;

        if (text.Length > MaxTextLengthLimit)
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            return ToolResult<byte[]>.Fail(
                ToolErrorKind.TooLarge,
                HmacErrorCodes.InputTooLarge,
                $"Input length {text.Length} exceeds {MaxTextLengthLimit} characters.");
        }

        byte[] messageBytes = Encoding.UTF8.GetBytes(text);

        try
        {
            using HMAC hmac = CreateHmac(descriptor!.Kind, keyBytes);
            return ToolResult<byte[]>.Ok(hmac.ComputeHash(messageBytes));
        }
        catch (CryptographicException ex)
        {
            return ToolResult<byte[]>.Fail(ToolErrorKind.Unexpected, HmacErrorCodes.AlgorithmUnavailable, ex.Message);
        }
        finally
        {
            // Anahtar ve mesaj tamponları bellekte kalmamalı.
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(messageBytes);
        }
    }

    public bool TryParseMac(string? value, out byte[] mac)
    {
        mac = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = HexFormatting.NormalizeHex(value);

        if (normalized.Length > 0 && normalized.Length % 2 == 0 && IsAllHex(normalized))
        {
            mac = Convert.FromHexString(normalized);
            return mac.Length > 0;
        }

        // Onaltılık değilse Base64 olarak denenir.
        Span<byte> buffer = stackalloc byte[512];

        if (Convert.TryFromBase64String(value.Trim(), buffer, out int written) && written > 0)
        {
            mac = buffer[..written].ToArray();
            return true;
        }

        return false;
    }

    private static HMAC CreateHmac(HashAlgorithmKind kind, byte[] key) => kind switch
    {
        HashAlgorithmKind.Md5 => new HMACMD5(key),
        HashAlgorithmKind.Sha1 => new HMACSHA1(key),
        HashAlgorithmKind.Sha256 => new HMACSHA256(key),
        HashAlgorithmKind.Sha384 => new HMACSHA384(key),
        HashAlgorithmKind.Sha512 => new HMACSHA512(key),
        HashAlgorithmKind.Sha3_256 => new HMACSHA3_256(key),
        HashAlgorithmKind.Sha3_512 => new HMACSHA3_512(key),
        _ => throw new CryptographicException($"Unsupported HMAC algorithm: {kind}")
    };

    private static bool TryGetKeyBytes(string key, HmacKeyFormat keyFormat, out byte[] keyBytes, out ToolResult<byte[]>? failure)
    {
        failure = null;

        if (keyFormat == HmacKeyFormat.Text)
        {
            keyBytes = Encoding.UTF8.GetBytes(key);
            return true;
        }

        string normalized = HexFormatting.NormalizeHex(key);

        if (normalized.Length == 0 || normalized.Length % 2 != 0 || !IsAllHex(normalized))
        {
            keyBytes = Array.Empty<byte>();
            failure = ToolResult<byte[]>.Fail(
                ToolErrorKind.Validation,
                HmacErrorCodes.InvalidHexKey,
                "The key is not valid hexadecimal; use an even number of hex digits or switch the key format to text.");
            return false;
        }

        keyBytes = Convert.FromHexString(normalized);
        return true;
    }

    private static bool IsAllHex(string value)
    {
        foreach (char character in value)
        {
            if (!HexDigits.TryParse(character, out _))
                return false;
        }

        return value.Length > 0;
    }

    private bool TryResolve(HashAlgorithmKind kind, out HashAlgorithmDescriptor? descriptor, out ToolResult<byte[]>? failure)
    {
        failure = null;
        descriptor = null;

        foreach (HashAlgorithmDescriptor candidate in AvailableAlgorithms.Value)
        {
            if (candidate.Kind != kind)
                continue;

            descriptor = candidate;
            return true;
        }

        foreach (HashAlgorithmDescriptor candidate in Candidates)
        {
            if (candidate.Kind != kind)
                continue;

            failure = ToolResult<byte[]>.Fail(
                ToolErrorKind.Unexpected,
                HmacErrorCodes.AlgorithmUnavailable,
                $"{candidate.DisplayName} is not available on this system.");
            return false;
        }

        failure = ToolResult<byte[]>.Fail(
            ToolErrorKind.Validation,
            HmacErrorCodes.UnsupportedAlgorithm,
            $"Unknown HMAC algorithm: {kind}.");
        return false;
    }

    private static HashAlgorithmDescriptor[] Probe()
    {
        var available = new List<HashAlgorithmDescriptor>(Candidates.Length);

        foreach (HashAlgorithmDescriptor candidate in Candidates)
        {
            try
            {
                using HMAC hmac = CreateHmac(candidate.Kind, new byte[] { 1 });
                hmac.ComputeHash(Array.Empty<byte>());
                available.Add(candidate);
            }
            catch (CryptographicException)
            {
                // Bu işletim sisteminde HMAC varyantı yok; listeye eklenmez.
            }
            catch (ArgumentException)
            {
                // Algoritma adı tanınmadı; listeye eklenmez.
            }
        }

        return available.ToArray();
    }
}
