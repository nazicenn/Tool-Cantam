using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class LoremIpsumServiceTests
{
    private readonly LoremIpsumService _service = new();

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsText()
    {
        var r = await _service.GenerateAsync(new LoremOptions(3, 5, true), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.ParagraphCount);
        Assert.StartsWith("Lorem ipsum", r.Value.Text);
    }

    [Fact]
    public async Task GenerateAsync_StartWithLoremFalse_NoPrefix()
    {
        var r = await _service.GenerateAsync(new LoremOptions(1, 1, false), TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.DoesNotContain("Lorem ipsum dolor sit amet,", r.Value!.Text);
    }

    [Fact]
    public async Task GenerateAsync_InvalidParagraphs_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(new LoremOptions(0, 5, true), TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_InvalidSentences_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(new LoremOptions(3, 0, true), TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.GenerateAsync(new LoremOptions(10, 10, true), cts.Token);
        Assert.False(r.IsSuccess);
    }
}