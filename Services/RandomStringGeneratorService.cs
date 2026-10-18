using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// RandomNumberGenerator (CSPRNG) ile rastgele metin üretir. Random sınıfı asla kullanılmaz.
/// Her havuzdan en az bir karakter garanti edilebilir (EnsureEachType).
/// </summary>
public sealed class RandomStringGeneratorService : IRandomStringGeneratorService
{
    public const int MinLengthLimit = 4;
    public const int MaxLengthLimit = 256;
    public const int MinCountLimit = 1;
    public const int MaxCountLimit = 100;

    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?";
    private const string Similar = "Il1O0o";

    public int MinLength => MinLengthLimit;
    public int MaxLength => MaxLengthLimit;
    public int MinCount => MinCountLimit;
    public int MaxCount => MaxCountLimit;

    public Task<ToolResult<RandomStringReport>> GenerateAsync(
        RandomStringOptions options, CancellationToken cancellationToken)
    {
        if (options is null)
            return Fail<RandomStringReport>(ToolErrorKind.Validation,
                RandomStringGeneratorErrorCodes.InvalidLength, "Options are null.");

        if (options.Length < MinLengthLimit || options.Length > MaxLengthLimit)
            return Fail<RandomStringReport>(ToolErrorKind.Validation,
                RandomStringGeneratorErrorCodes.InvalidLength,
                $"Length must be {MinLengthLimit}..{MaxLengthLimit} (got {options.Length}).");

        if (options.Count < MinCountLimit || options.Count > MaxCountLimit)
            return Fail<RandomStringReport>(ToolErrorKind.Validation,
                RandomStringGeneratorErrorCodes.InvalidCount,
                $"Count must be {MinCountLimit}..{MaxCountLimit} (got {options.Count}).");

        var pools = BuildPools(options);
        if (pools.Count == 0)
            return Fail<RandomStringReport>(ToolErrorKind.Validation,
                RandomStringGeneratorErrorCodes.NoCharacterSet,
                "Select at least one character set.");

        if (options.EnsureEachType && options.Length < pools.Count)
            return Fail<RandomStringReport>(ToolErrorKind.Validation,
                RandomStringGeneratorErrorCodes.InvalidLength,
                $"Length {options.Length} is smaller than the number of selected character sets ({pools.Count}).");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var combined = string.Concat(pools);
            var items = new List<string>(options.Count);
            var sb = new StringBuilder();

            for (int i = 0; i < options.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var s = GenerateOne(options, pools, combined, cancellationToken);
                items.Add(s);
                sb.AppendLine(s);
            }

            sw.Stop();
            int entropy = (int)(options.Length * Math.Log2(combined.Length));
            var report = new RandomStringReport(items, sb.ToString().TrimEnd(), sw.Elapsed, entropy);
            return Task.FromResult(ToolResult<RandomStringReport>.Ok(report));
        }
        catch (OperationCanceledException)
        {
            return Fail<RandomStringReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<RandomStringReport>(ToolErrorKind.Unexpected,
                RandomStringGeneratorErrorCodes.Unexpected, ex.Message);
        }
    }

    private static List<string> BuildPools(RandomStringOptions o)
    {
        var pools = new List<string>();
        if (o.IncludeUppercase) pools.Add(FilterSimilar(Upper, o.ExcludeSimilar));
        if (o.IncludeLowercase) pools.Add(FilterSimilar(Lower, o.ExcludeSimilar));
        if (o.IncludeDigits) pools.Add(FilterSimilar(Digits, o.ExcludeSimilar));
        if (o.IncludeSymbols) pools.Add(FilterSimilar(Symbols, o.ExcludeSimilar));
        return pools.Where(p => p.Length > 0).ToList();
    }

    private static string FilterSimilar(string source, bool exclude)
        => exclude ? new string(source.Where(c => !Similar.Contains(c)).ToArray()) : source;

    private static string GenerateOne(
        RandomStringOptions o, List<string> pools, string combined, CancellationToken ct)
    {
        var chars = new char[o.Length];
        int idx = 0;

        if (o.EnsureEachType)
        {
            foreach (var pool in pools)
            {
                ct.ThrowIfCancellationRequested();
                chars[idx++] = RandomChar(pool);
            }
        }

        while (idx < o.Length)
        {
            ct.ThrowIfCancellationRequested();
            chars[idx++] = RandomChar(combined);
        }

        // Fisher-Yates (CSPRNG)
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char RandomChar(string pool)
        => pool[RandomNumberGenerator.GetInt32(pool.Length)];

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}