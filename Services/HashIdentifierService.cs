using System.Collections.Generic;
using System.Text.RegularExpressions;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

public class HashIdentifierService : IHashIdentifierService
{
    public List<string> Identify(string hash)
    {
        var results = new List<string>();
        var trimmedHash = hash.Trim();

        if (string.IsNullOrWhiteSpace(trimmedHash))
            return new List<string> { "Empty input" };

        int length = trimmedHash.Length;

        // bcrypt: $2a$, $2b$, $2y$, $2x$ ile başlar ve 60 karakter uzunluğunda
        if (Regex.IsMatch(trimmedHash, @"^\$2[abyx]\$\d{2}\$"))
        {
            results.Add("bcrypt");
            return results;
        }

        // MD5 (32 karakter hex)
        if (length == 32 && IsHex(trimmedHash))
            results.Add("MD5");

        // SHA-1 (40 karakter hex)
        if (length == 40 && IsHex(trimmedHash))
            results.Add("SHA-1");

        // SHA-256 (64 karakter hex)
        if (length == 64 && IsHex(trimmedHash))
            results.Add("SHA-256");

        // SHA-384 (96 karakter hex)
        if (length == 96 && IsHex(trimmedHash))
            results.Add("SHA-384");

        // SHA-512 (128 karakter hex)
        if (length == 128 && IsHex(trimmedHash))
            results.Add("SHA-512");

        // NTLM (32 karakter, hex değil, genelde MD5 gibi ama farklı)
        if (length == 32 && !IsHex(trimmedHash) && IsAlphaNumeric(trimmedHash))
            results.Add("NTLM");

        // MD5 base64 formatı
        if (length == 24 && Regex.IsMatch(trimmedHash, @"^[A-Za-z0-9+/]+={0,2}$"))
            results.Add("MD5 (base64)");

        if (results.Count == 0)
            results.Add("Unknown / Unsupported hash format");

        return results;
    }

    private bool IsHex(string input)
    {
        return Regex.IsMatch(input, @"^[0-9a-fA-F]+$");
    }

    private bool IsAlphaNumeric(string input)
    {
        return Regex.IsMatch(input, @"^[0-9a-zA-Z]+$");
    }
}