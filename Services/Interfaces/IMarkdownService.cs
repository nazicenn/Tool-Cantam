using System;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;

namespace tool_cantam.Services.Interfaces;

public interface IMarkdownService
{
    int MaxInputLength { get; }
    Task<ToolResult<MarkdownReport>> ToHtmlAsync(string? markdown, MarkdownOptions options, CancellationToken ct);
}

public sealed record MarkdownOptions(bool WrapInHtml, bool UseAdvancedExtensions);
public sealed record MarkdownReport(string Html, int InputLength, int OutputLength, int WordCount, TimeSpan Elapsed);