using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Kriptografik güvenli secret (API key, JWT secret, token, password, hex).</summary>
public sealed class SecretGeneratorService : ISecretGeneratorService
{
    public const int MinLengthLimit = 8;
    public const int MaxLengthLimit = 256;
    public const int MinCountLimit = 1;
    public const int MaxCountLimit = 100;

    private const string AlphanumUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string AlphanumMixed = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AllChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}";

    public int MinLength => MinLengthLimit;
    public int MaxLength => MaxLengthLimit;

    public Task<ToolResult<SecretReport>> GenerateAsync(SecretOptions o, CancellationToken ct)
    {
        if (o is null || o.Length < MinLengthLimit || o.Length > MaxLengthLimit)
            return Fail<SecretReport>(ToolErrorKind.Validation, SecretGeneratorErrorCodes.InvalidLength,
                $"Length must be {MinLengthLimit}..{MaxLengthLimit}.");
        if (o.Count < MinCountLimit || o.Count > MaxCountLimit)
            return Fail<SecretReport>(ToolErrorKind.Validation, SecretGeneratorErrorCodes.InvalidCount,
                $"Count must be {MinCountLimit}..{MaxCountLimit}.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var items = new List<string>(o.Count);
            var sb = new StringBuilder();
            for (int i = 0; i < o.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var s = GenerateOne(o);
                items.Add(s);
                sb.AppendLine(s);
            }

            sw.Stop();
            return Task.FromResult(ToolResult<SecretReport>.Ok(new SecretReport(items, sb.ToString().TrimEnd(), o.Length, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<SecretReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<SecretReport>(ToolErrorKind.Unexpected, SecretGeneratorErrorCodes.Unexpected, ex.Message); }
    }

    private static string GenerateOne(SecretOptions o) => o.Kind switch
    {
        SecretKind.ApiKey => RandomFromAlphabet(AlphanumUpper, o.Length),
        SecretKind.JwtSecret => RandomFromAlphabet(AllChars, o.Length),
        SecretKind.RandomToken => Convert.ToBase64String(RandomNumberGenerator.GetBytes(o.Length))
            .Replace("+", "").Replace("/", "").Replace("=", "")[..Math.Min(o.Length, 32)],
        SecretKind.Password => RandomFromAlphabet(AllChars, o.Length),
        SecretKind.HexKey => Convert.ToHexString(RandomNumberGenerator.GetBytes(o.Length / 2)).ToLowerInvariant(),
        _ => RandomFromAlphabet(AlphanumMixed, o.Length),
    };

    private static string RandomFromAlphabet(string alphabet, int length)
    {
        var result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return new string(result);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}