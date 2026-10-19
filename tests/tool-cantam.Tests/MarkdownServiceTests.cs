using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class MarkdownServiceTests
{
    private readonly MarkdownService _service = new();
    private static MarkdownOptions Opts(bool wrap = false, bool adv = true) => new(wrap, adv);

    [Fact] public async Task ToHtmlAsync_Header_Works() { var r = await _service.ToHtmlAsync("# Hello", Opts(), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Contains("<h1", r.Value!.Html); }
    [Fact] public async Task ToHtmlAsync_Bold_Works() { var r = await _service.ToHtmlAsync("**bold**", Opts(), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Contains("<strong>", r.Value!.Html); }
    [Fact] public async Task ToHtmlAsync_WrapInHtml_Works() { var r = await _service.ToHtmlAsync("# Test", Opts(wrap: true), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Contains("<html", r.Value!.Html); }
    [Fact] public async Task ToHtmlAsync_Empty_ReturnsValidation() { var r = await _service.ToHtmlAsync("", Opts(), TestContext.Current.CancellationToken); Assert.False(r.IsSuccess); Assert.Equal(ToolErrorKind.Validation, r.ErrorKind); }
    [Fact] public async Task ToHtmlAsync_CountsWords() { var r = await _service.ToHtmlAsync("hello world test", Opts(), TestContext.Current.CancellationToken); Assert.True(r.IsSuccess); Assert.Equal(3, r.Value!.WordCount); }
}