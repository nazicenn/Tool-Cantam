using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Lorem ipsum üretici. CSPRNG ile kelime seçimi.</summary>
public sealed class LoremIpsumService : ILoremIpsumService
{
    public const int MinParagraphsLimit = 1;
    public const int MaxParagraphsLimit = 50;
    public const int MinSentencesLimit = 1;
    public const int MaxSentencesLimit = 20;

    public int MinParagraphs => MinParagraphsLimit;
    public int MaxParagraphs => MaxParagraphsLimit;
    public int MinSentences => MinSentencesLimit;
    public int MaxSentences => MaxSentencesLimit;

    private static readonly string[] Words =
    {
        "lorem","ipsum","dolor","sit","amet","consectetur","adipiscing","elit","sed","do",
        "eiusmod","tempor","incididunt","ut","labore","et","dolore","magna","aliqua","enim",
        "ad","minim","veniam","quis","nostrud","exercitation","ullamco","laboris","nisi","aliquip",
        "ex","ea","commodo","consequat","duis","aute","irure","in","reprehenderit","voluptate",
        "velit","esse","cillum","fugiat","nulla","pariatur","excepteur","sint","occaecat","cupidatat",
        "non","proident","sunt","culpa","qui","officia","deserunt","mollit","anim","id",
        "est","laborum","curabitur","pretium","tincidunt","lacus","gravida","orci","odio","nullam",
        "varius","turpis","condimentum","lobortis","feugiat","vivamus","elementum","semper","aenean","vulputate",
    };

    public Task<ToolResult<LoremReport>> GenerateAsync(LoremOptions o, CancellationToken ct)
    {
        if (o is null || o.ParagraphCount < MinParagraphsLimit || o.ParagraphCount > MaxParagraphsLimit)
            return Fail<LoremReport>(ToolErrorKind.Validation, LoremIpsumErrorCodes.InvalidParagraphs,
                $"Paragraphs must be {MinParagraphsLimit}..{MaxParagraphsLimit}.");
        if (o.SentenceCount < MinSentencesLimit || o.SentenceCount > MaxSentencesLimit)
            return Fail<LoremReport>(ToolErrorKind.Validation, LoremIpsumErrorCodes.InvalidSentences,
                $"Sentences must be {MinSentencesLimit}..{MaxSentencesLimit}.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();
            var sb = new StringBuilder();
            int totalWords = 0;

            for (int p = 0; p < o.ParagraphCount; p++)
            {
                for (int s = 0; s < o.SentenceCount; s++)
                {
                    ct.ThrowIfCancellationRequested();
                    string sentence = GenerateSentence();
                    if (p == 0 && s == 0 && o.StartWithLorem)
                        sentence = "Lorem ipsum dolor sit amet, " + char.ToLowerInvariant(sentence[0]) + sentence[1..];
                    sb.Append(sentence).Append(' ');
                    totalWords += sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                }
                sb.AppendLine().AppendLine();
            }

            sw.Stop();
            return Task.FromResult(ToolResult<LoremReport>.Ok(
                new LoremReport(sb.ToString().Trim(), o.ParagraphCount, totalWords, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<LoremReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<LoremReport>(ToolErrorKind.Unexpected, LoremIpsumErrorCodes.Unexpected, ex.Message); }
    }

    private static string GenerateSentence()
    {
        int count = System.Security.Cryptography.RandomNumberGenerator.GetInt32(8, 18);
        var words = new string[count];
        for (int i = 0; i < count; i++)
            words[i] = Words[System.Security.Cryptography.RandomNumberGenerator.GetInt32(Words.Length)];
        var s = string.Join(" ", words);
        return char.ToUpperInvariant(s[0]) + s[1..] + ".";
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}