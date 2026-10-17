using System;
using System.Collections.Generic;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// Metin istatistikleri sözleşmesi. Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner;
/// boyut sınırı ve geçersiz Unicode sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface ITextCounterService
{
    /// <summary>İşlenecek en büyük karakter sayısı.</summary>
    int MaxTextLength { get; }

    /// <summary>Metnin tüm istatistiklerini hesaplar.</summary>
    ToolResult<TextCounterReport> Count(string? text);
}

/// <summary>Metin istatistiklerinin raporu.</summary>
public sealed record TextCounterReport(
    int CharacterCount,
    int CharacterCountNoSpaces,
    int WordCount,
    int LineCount,
    int ParagraphCount,
    int SentenceCount,
    TimeSpan ReadingTime,
    TimeSpan SpeakingTime,
    IReadOnlyList<WordFrequency> TopWords);

/// <summary>Bir kelimenin frekansı.</summary>
public sealed record WordFrequency(string Word, int Count);