using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class WordCounterServiceTests
{
    private readonly WordCounterService _service = new();

    [Fact]
    public async Task AnalyzeAsync_Simple_CountsCorrectly()
    {
        var r = await _service.AnalyzeAsync("Hello world. This is a test.", TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(6, r.Value!.Words);
        Assert.Equal(2, r.Value.Sentences);
    }

    [Fact]
    public async Task AnalyzeAsync_Empty_ReturnsZero()
    {
        var r = await _service.AnalyzeAsync("", TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(0, r.Value!.Words);
        Assert.Equal(0, r.Value.Characters);
    }

    [Fact]
    public async Task AnalyzeAsync_MultipleParagraphs_CountsCorrectly()
    {
        var r = await _service.AnalyzeAsync("Para 1.\n\nPara 2.", TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value!.Paragraphs);
    }

    [Fact]
    public async Task AnalyzeAsync_ReadingTime_CalculatedCorrectly()
    {
        var text = string.Join(" ", System.Linq.Enumerable.Repeat("word", 200));
        var r = await _service.AnalyzeAsync(text, TestContext.Current.CancellationToken);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, (int)r.Value!.ReadingTime.TotalMinutes);
    }

    [Fact]
    public async Task AnalyzeAsync_TooLarge_ReturnsTooLarge()
    {
        var huge = new string('a', WordCounterService.MaxInputLengthLimit + 1);
        var r = await _service.AnalyzeAsync(huge, TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.TooLarge, r.ErrorKind);
    }
}