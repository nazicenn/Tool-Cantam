using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Markdig;
using tool_cantam.Core.Models;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;

namespace tool_cantam.Services;

/// <summary>Markdig ile Markdown → HTML.</summary>
public sealed class MarkdownService : IMarkdownService
{
    public const int MaxInputLengthLimit = 10 * 1024 * 1024;
    public int MaxInputLength => MaxInputLengthLimit;

    private const string HtmlWrapper = @"<html><head><meta charset='utf-8'/><style>
body{font-family:'Segoe UI',sans-serif;padding:20px;line-height:1.6;background:#1e1e1e;color:#e0e0e0;}
h1,h2,h3{color:#00ff41;} a{color:#00ff41;}
code{background:#2d2d2d;padding:2px 6px;border-radius:4px;font-family:'Consolas',monospace;}
pre{background:#2d2d2d;padding:15px;border-radius:8px;overflow-x:auto;}
blockquote{border-left:3px solid #00ff41;margin:10px 0;padding-left:15px;color:#888;}
table{border-collapse:collapse;width:100%;} th,td{border:1px solid #444;padding:8px;}
th{background:#2d2d2d;}
</style></head><body>";

    public Task<ToolResult<MarkdownReport>> ToHtmlAsync(string? markdown, MarkdownOptions options, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return Fail<MarkdownReport>(ToolErrorKind.Validation, MarkdownErrorCodes.EmptyInput, "Input is empty.");
        if (markdown.Length > MaxInputLengthLimit)
            return Fail<MarkdownReport>(ToolErrorKind.TooLarge, MarkdownErrorCodes.InputTooLarge, "Input too large.");

        try
        {
            ct.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            var builder = new MarkdownPipelineBuilder();
            if (options.UseAdvancedExtensions)
                builder.UseAdvancedExtensions();
            var pipeline = builder.Build();

            var html = Markdown.ToHtml(markdown, pipeline);
            if (options.WrapInHtml)
                html = HtmlWrapper + html + "</body></html>";

            var wordCount = markdown.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            sw.Stop();

            return Task.FromResult(ToolResult<MarkdownReport>.Ok(
                new MarkdownReport(html, markdown.Length, html.Length, wordCount, sw.Elapsed)));
        }
        catch (OperationCanceledException) { return Fail<MarkdownReport>(ToolErrorKind.Cancelled, "L.Common.Error.Cancelled", "Cancelled."); }
        catch (Exception ex) { return Fail<MarkdownReport>(ToolErrorKind.Unexpected, MarkdownErrorCodes.Unexpected, ex.Message); }
    }

    private static Task<ToolResult<T>> Fail<T>(ToolErrorKind k, string c, string m) => Task.FromResult(ToolResult<T>.Fail(k, c, m));
}