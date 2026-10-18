using tool_cantam.Services.ErrorCodes;
using tool_cantam.Core.Enums;
using tool_cantam.Core.Models;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Bir özetin algoritmasını biçiminden çıkarır: uzunluk, karakter kümesi ve ayırt edici önek.
/// <para>
/// Kural: yalnızca biçimden tanımlama <b>asla kesin değildir</b>. Aynı uzunluktaki algoritmalar
/// (ör. 32 hex: MD5, NTLM, MD4) ayırt edilemez ve bu yüzden birden çok aday olarak listelenir.
/// Kesin sonuç yalnızca ayırt edici öneklerde (<c>$2b$</c>, <c>$argon2id$</c>, <c>{SSHA}</c> ...) verilir.
/// Düzenli ifade kullanılmaz; tarama elle yapılır.
/// </para>
/// </summary>
public sealed class HashIdentifierService : IHashIdentifierService
{
    public const int MaxInputLengthLimit = 4096;

    private static readonly Dictionary<int, string[]> HexShapes = new()
    {
        [8] = new[] { "CRC-32" },
        [16] = new[] { "CRC-64", "MySQL323" },
        [32] = new[] { "MD5", "NTLM", "MD4" },
        [40] = new[] { "SHA-1", "RIPEMD-160", "MySQL5" },
        [56] = new[] { "SHA-224" },
        [64] = new[] { "SHA-256", "SHA3-256", "BLAKE2s-256" },
        [96] = new[] { "SHA-384", "SHA3-384" },
        [128] = new[] { "SHA-512", "SHA3-512", "BLAKE2b-512", "Whirlpool" }
    };

    private static readonly Dictionary<int, string[]> Base64Shapes = new()
    {
        [24] = new[] { "MD5", "NTLM" },
        [28] = new[] { "SHA-1", "RIPEMD-160" },
        [44] = new[] { "SHA-256", "SHA3-256" },
        [88] = new[] { "SHA-512", "SHA3-512" }
    };

    /// <summary>Ayırt edici önekler: bunlar görüldüğünde algoritma kesindir.</summary>
    private static readonly (string Prefix, string Algorithm)[] DistinctivePrefixes =
    {
        ("$2a$", "bcrypt"),
        ("$2b$", "bcrypt"),
        ("$2x$", "bcrypt"),
        ("$2y$", "bcrypt"),
        ("$apr1$", "Apache MD5 crypt"),
        ("$1$", "MD5 crypt"),
        ("$5$", "SHA-256 crypt"),
        ("$6$", "SHA-512 crypt"),
        ("$argon2id$", "Argon2id"),
        ("$argon2i$", "Argon2i"),
        ("$argon2d$", "Argon2d"),
        ("$pbkdf2", "PBKDF2"),
        ("{SSHA}", "LDAP SSHA"),
        ("{SMD5}", "LDAP SMD5"),
        ("{SHA}", "LDAP SHA"),
        ("{MD5}", "LDAP MD5"),
        ("{CRYPT}", "LDAP crypt")
    };

    public int MaxInputLength => MaxInputLengthLimit;

    public ToolResult<HashIdentificationResult> Identify(string? value)
    {
        string input = (value ?? string.Empty).Trim();

        if (input.Length == 0)
        {
            return ToolResult<HashIdentificationResult>.Fail(
                ToolErrorKind.Validation,
                HashIdentifierErrorCodes.EmptyInput,
                "Input is empty.");
        }

        if (input.Length > MaxInputLengthLimit)
        {
            return ToolResult<HashIdentificationResult>.Fail(
                ToolErrorKind.TooLarge,
                HashIdentifierErrorCodes.InputTooLong,
                $"Input length {input.Length} exceeds {MaxInputLengthLimit} characters.");
        }

        // Ayırt edici önek: kesin sonuç.
        foreach ((string prefix, string algorithm) in DistinctivePrefixes)
        {
            if (input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return ToolResult<HashIdentificationResult>.Ok(new HashIdentificationResult(
                    input.Length,
                    DetectCharacterSet(input),
                    HasDistinctivePrefix: true,
                    new[] { new HashCandidate(algorithm, HashIdentifierConfidence.Certain) }));
            }
        }

        // MySQL yerel parola biçimi: '*' + 40 hex.
        if (input.Length == 41 && input[0] == '*' && IsAllHex(input.AsSpan(1)))
        {
            return ToolResult<HashIdentificationResult>.Ok(new HashIdentificationResult(
                input.Length,
                HashCharacterSet.Hex,
                HasDistinctivePrefix: true,
                new[] { new HashCandidate("MySQL native password", HashIdentifierConfidence.Likely) }));
        }

        // "0x" öneki yalnızca gösterim farkıdır; uzunluk hesabından çıkarılır.
        string candidate = input;
        if (candidate.Length > 2 && candidate[0] == '0' && (candidate[1] == 'x' || candidate[1] == 'X'))
            candidate = candidate[2..];

        HashCharacterSet characterSet = DetectCharacterSet(candidate);
        string[] algorithms = ResolveAlgorithms(candidate, characterSet);

        var candidates = new List<HashCandidate>(algorithms.Length);
        HashIdentifierConfidence confidence = algorithms.Length == 1
            ? HashIdentifierConfidence.Likely
            : HashIdentifierConfidence.Possible;

        foreach (string algorithm in algorithms)
            candidates.Add(new HashCandidate(algorithm, confidence));

        return ToolResult<HashIdentificationResult>.Ok(new HashIdentificationResult(
            candidate.Length,
            characterSet,
            HasDistinctivePrefix: false,
            candidates));
    }

    private static string[] ResolveAlgorithms(string value, HashCharacterSet characterSet)
    {
        // Unix crypt (DES): 13 karakterlik özel biçim.
        if (characterSet == HashCharacterSet.DesCrypt && value.Length == 13)
            return new[] { "DES crypt" };

        Dictionary<int, string[]> table = characterSet switch
        {
            HashCharacterSet.Hex => HexShapes,

            // Alfanümerik bir metin de geçerli Base64 olabilir (Base64 alfabesi alfanümerik kümesi kapsar),
            // bu yüzden Base64 özet uzunlukları burada da denenir.
            HashCharacterSet.Base64 or HashCharacterSet.Alphanumeric => Base64Shapes,

            _ => new Dictionary<int, string[]>()
        };

        return table.TryGetValue(value.Length, out string[]? algorithms) ? algorithms : Array.Empty<string>();
    }

    private static HashCharacterSet DetectCharacterSet(string value)
    {
        if (value.Length == 0)
            return HashCharacterSet.Other;

        if (IsAllHex(value.AsSpan()))
            return HashCharacterSet.Hex;

        bool base64 = true;
        bool desCrypt = true;
        bool alphanumeric = true;
        bool hasBase64Punctuation = false;
        int firstPaddingIndex = -1;

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];

            bool isLetterOrDigit =
                (character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9');

            if (!isLetterOrDigit)
                alphanumeric = false;

            if (character == '=')
            {
                if (firstPaddingIndex < 0)
                    firstPaddingIndex = index;

                hasBase64Punctuation = true;
                desCrypt = false;
                continue;
            }

            // Dolgudan sonra veri karakteri gelemez.
            if (firstPaddingIndex >= 0)
                base64 = false;

            if (character is '+' or '/')
                hasBase64Punctuation = true;

            if (!isLetterOrDigit && character != '+' && character != '/')
                base64 = false;

            if (!isLetterOrDigit && character != '.' && character != '/')
                desCrypt = false;
        }

        // Dolgu en fazla iki karakter olabilir.
        if (base64 && firstPaddingIndex >= 0 && value.Length - firstPaddingIndex > 2)
            base64 = false;

        // 13 karakterlik crypt biçimi DES crypt'tir; Base64 özet uzunlukları 24/28/44/88'dir.
        if (value.Length == 13 && desCrypt)
            return HashCharacterSet.DesCrypt;

        // Ayırt edici Base64 işareti varsa (dolgu, '+' veya '/') Base64 olarak raporlanır.
        if (base64 && hasBase64Punctuation)
            return HashCharacterSet.Base64;

        if (alphanumeric)
            return HashCharacterSet.Alphanumeric;

        if (base64)
            return HashCharacterSet.Base64;

        if (desCrypt)
            return HashCharacterSet.DesCrypt;

        return HashCharacterSet.Other;
    }

    private static bool IsAllHex(ReadOnlySpan<char> value)
    {
        if (value.Length == 0)
            return false;

        foreach (char character in value)
        {
            bool isHexDigit =
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F');

            if (!isHexDigit)
                return false;
        }

        return true;
    }
}
