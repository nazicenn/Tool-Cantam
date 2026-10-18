using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Regex test. 2 saniye timeout + 10.000 eşleşme sınırı ile DoS korumalı.
/// </summary>
public sealed class RegexTesterService : IRegexTesterService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public const int MaxMatchCountLimit = 10_000;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public int MaxInputLength => MaxInputLengthLimit;
    public int MaxMatchCount => MaxMatchCountLimit;
    public TimeSpan RegexTimeout => Timeout;

    public Task<ToolResult<RegexReport>> TestAsync(
        string? pattern, string? input,
        RegexTestOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return Fail<RegexReport>(ToolErrorKind.Validation,
                RegexTesterErrorCodes.EmptyPattern, "Pattern is empty.");

        if (string.IsNullOrEmpty(input))
            return Fail<RegexReport>(ToolErrorKind.Validation,
                RegexTesterErrorCodes.EmptyInput, "Input is empty.");

        if (input.Length > MaxInputLengthLimit)
            return Fail<RegexReport>(ToolErrorKind.TooLarge,
                RegexTesterErrorCodes.InputTooLarge,
                $"Input length {input.Length} exceeds {MaxInputLengthLimit}.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var regex = new Regex(pattern, BuildOptions(options), Timeout);

            var matches = new List<RegexMatch>();
            foreach (Match m in regex.Matches(input))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (matches.Count >= MaxMatchCountLimit)
                    return Fail<RegexReport>(ToolErrorKind.TooLarge,
                        RegexTesterErrorCodes.TooManyMatches,
                        $"More than {MaxMatchCountLimit} matches.");

                var groups = new List<string>(m.Groups.Count);
                for (int i = 0; i < m.Groups.Count; i++)
                    groups.Add(m.Groups[i].Value);

                matches.Add(new RegexMatch(m.Index, m.Length, m.Value, groups));
            }

            var highlighted = regex.Replace(input, m => $"[{m.Value}]");
            var replaced = options.ReplaceMode
                ? regex.Replace(input, options.ReplaceWith ?? string.Empty)
                : string.Empty;

            sw.Stop();
            return Task.FromResult(ToolResult<RegexReport>.Ok(
                new RegexReport(matches, highlighted, replaced, matches.Count, sw.Elapsed)));
        }
        catch (RegexMatchTimeoutException)
        {
            return Fail<RegexReport>(ToolErrorKind.Unexpected,
                RegexTesterErrorCodes.Timeout,
                $"Regex execution timed out after {Timeout.TotalSeconds}s.");
        }
        catch (ArgumentException ex)
        {
            return Fail<RegexReport>(ToolErrorKind.InvalidFormat,
                RegexTesterErrorCodes.InvalidPattern, ex.Message);
        }
        catch (OperationCanceledException)
        {
            return Fail<RegexReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (Exception ex)
        {
            return Fail<RegexReport>(ToolErrorKind.Unexpected,
                RegexTesterErrorCodes.Unexpected, ex.Message);
        }
    }

    public IReadOnlyList<RegexSample> GetSamples() => new[]
    {
        new RegexSample("L.RegexTester.Sample.Email",
            @"\b[\w.+-]+@[\w-]+\.[\w.-]+\b",
            "john@example.com, jane.doe@test.co.uk, invalid-email",
            "[EMAIL]"),
        new RegexSample("L.RegexTester.Sample.Url",
            @"https?://[\w.-]+(?:/[\w./?%&=#-]*)?",
            "Visit https://example.com/page or http://test.org",
            "[URL]"),
        new RegexSample("L.RegexTester.Sample.Date",
            @"\d{4}-\d{2}-\d{2}",
            "Created 2024-12-08, updated 2025-01-15",
            "[DATE]"),
        new RegexSample("L.RegexTester.Sample.Ip",
            @"\b(?:\d{1,3}\.){3}\d{1,3}\b",
            "Local IPs: 192.168.1.1 and 10.0.0.1",
            "[IP]"),
        new RegexSample("L.RegexTester.Sample.TurkishWord",
            @"\b\w+ç\w*\b",
            "çay, çiçek, güzel, çalışkan",
            "[Ç]"),
    };

    private static RegexOptions BuildOptions(RegexTestOptions o)
    {
        var opts = RegexOptions.CultureInvariant;
        if (o.IgnoreCase) opts |= RegexOptions.IgnoreCase;
        if (o.Multiline) opts |= RegexOptions.Multiline;
        if (o.Singleline) opts |= RegexOptions.Singleline;
        if (o.ExplicitCapture) opts |= RegexOptions.ExplicitCapture;
        if (o.IgnorePatternWhitespace) opts |= RegexOptions.IgnorePatternWhitespace;
        return opts;
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}