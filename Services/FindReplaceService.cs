using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>
/// Regex tabanlı bul-değiştir. MatchWholeWord regex \b yerine Unicode-aware lookaround kullanır.
/// </summary>
public sealed class FindReplaceService : IFindReplaceService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    private const int MaxRegexMatchCount = 1_000_000; // DoS koruması

    public int MaxInputLength => MaxInputLengthLimit;

    public Task<ToolResult<FindReplaceReport>> ReplaceAsync(
        string? input, string? find, string? replace,
        FindReplaceOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input))
            return Fail<FindReplaceReport>(ToolErrorKind.Validation,
                FindReplaceErrorCodes.EmptyInput, "Input is empty.");

        if (input.Length > MaxInputLengthLimit)
            return Fail<FindReplaceReport>(ToolErrorKind.TooLarge,
                FindReplaceErrorCodes.InputTooLarge,
                $"Input length {input.Length} exceeds {MaxInputLengthLimit}.");

        if (string.IsNullOrEmpty(find))
            return Fail<FindReplaceReport>(ToolErrorKind.Validation,
                FindReplaceErrorCodes.EmptyFind, "Find text is empty.");

        if (options is null)
            return Fail<FindReplaceReport>(ToolErrorKind.Validation,
                FindReplaceErrorCodes.EmptyFind, "Options are null.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var (pattern, regexOptions) = BuildPattern(find, options);
            var regex = new Regex(pattern, regexOptions, TimeSpan.FromSeconds(2));

            // Say
            int matchCount = 0;
            foreach (Match m in regex.Matches(input))
            {
                cancellationToken.ThrowIfCancellationRequested();
                matchCount++;
                if (matchCount > MaxRegexMatchCount)
                    return Fail<FindReplaceReport>(ToolErrorKind.TooLarge,
                        FindReplaceErrorCodes.InputTooLarge,
                        "Too many matches.");
            }

            // Değiştir
            int replaceCount = 0;
            string output = regex.Replace(input, m =>
            {
                replaceCount++;
                return replace ?? string.Empty;
            });

            sw.Stop();
            return Task.FromResult(ToolResult<FindReplaceReport>.Ok(
                new FindReplaceReport(output, matchCount, replaceCount, pattern, sw.Elapsed)));
        }
        catch (OperationCanceledException)
        {
            return Fail<FindReplaceReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled.");
        }
        catch (ArgumentException ex) when (ex is RegexParseException || ex is ArgumentException)
        {
            return Fail<FindReplaceReport>(ToolErrorKind.InvalidFormat,
                FindReplaceErrorCodes.InvalidRegex, ex.Message);
        }
        catch (RegexMatchTimeoutException)
        {
            return Fail<FindReplaceReport>(ToolErrorKind.Unexpected,
                FindReplaceErrorCodes.InvalidRegex, "Regex execution timed out.");
        }
        catch (Exception ex)
        {
            return Fail<FindReplaceReport>(ToolErrorKind.Unexpected,
                FindReplaceErrorCodes.Unexpected, ex.Message);
        }
    }

    private static (string Pattern, RegexOptions Options) BuildPattern(string find, FindReplaceOptions o)
    {
        string pattern = o.UseRegex ? find : Regex.Escape(find);

        if (o.MatchWholeWord)
        {
            // Unicode-aware word boundary: \b sadece ASCII, bu yüzden lookaround kullan
            pattern = $@"(?<![\p{{L}}\p{{N}}]){pattern}(?![\p{{L}}\p{{N}}])";
        }

        var opts = RegexOptions.None;
        if (!o.MatchCase) opts |= RegexOptions.IgnoreCase;
        opts |= RegexOptions.CultureInvariant;

        return (pattern, opts);
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind kind, string code, string message)
        => Task.FromResult(ToolResult<T>.Fail(kind, code, message));
}