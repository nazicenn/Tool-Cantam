using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface ILoremIpsumService
{
    int MinParagraphs { get; }
    int MaxParagraphs { get; }
    int MinSentences { get; }
    int MaxSentences { get; }
    Task<ToolResult<LoremReport>> GenerateAsync(LoremOptions options, CancellationToken ct);
}

public sealed record LoremOptions(int ParagraphCount, int SentenceCount, bool StartWithLorem);
public sealed record LoremReport(string Text, int ParagraphCount, int WordCount, TimeSpan Elapsed);