using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IWordCounterService
{
    int MaxInputLength { get; }
    Task<ToolResult<WordCountReport>> AnalyzeAsync(string? input, CancellationToken ct);
}

/// <summary>Word Counter için kelime frekansı (TextCounter'dan farklı).</summary>
public sealed record WordCounterWordFreq(string Word, int Count);

public sealed record WordCountReport(
    int Characters, int CharactersNoSpaces, int Words,
    int Sentences, int Paragraphs, int Lines,
    TimeSpan ReadingTime, TimeSpan SpeakingTime,
    IReadOnlyList<WordCounterWordFreq> TopWords,
    TimeSpan Elapsed);