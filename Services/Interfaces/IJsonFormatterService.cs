using System;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

/// <summary>
/// JSON biçimlendirme ve doğrulama sözleşmesi.
/// Tüm sonuçlar <see cref="ToolResult{T}"/> ile döner; syntax hataları satır/kolon
/// bilgisiyle sınıflandırılmış hata olarak bildirilir.
/// </summary>
public interface IJsonFormatterService
{
    int MaxTextLength { get; }
    int MaxDepth { get; }

    ToolResult<JsonFormatReport> Format(string? json, int indentSize);
    ToolResult<JsonFormatReport> Minify(string? json);
    ToolResult<JsonFormatReport> Validate(string? json);
}

/// <summary>JSON biçimlendirme sonucu.</summary>
public sealed record JsonFormatReport(
    string Output,
    int NodeCount,
    int MaxDepth,
    int InputBytes,
    int OutputBytes,
    TimeSpan Elapsed);