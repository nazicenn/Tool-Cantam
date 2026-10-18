using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class CronParserServiceTests
{
    private readonly CronParserService _service = new();

    [Fact]
    public async Task ParseAsync_Valid_ReturnsFields()
    {
        var r = await _service.ParseAsync("0 0 * * *", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(5, r.Value!.Fields.Count);
        Assert.Equal("Every day at midnight (00:00)", r.Value.Summary);
    }

    [Fact]
    public async Task ParseAsync_InvalidParts_ReturnsValidation()
    {
        var r = await _service.ParseAsync("0 0 *", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ParseAsync_Empty_ReturnsValidation()
    {
        var r = await _service.ParseAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ParseAsync_EveryHour_HasSummary()
    {
        var r = await _service.ParseAsync("0 * * * *", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Contains("hour", r.Value!.Summary.ToLower());
    }

    [Fact]
    public async Task ParseAsync_ReturnsNextRuns()
    {
        var r = await _service.ParseAsync("*/15 * * * *", CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.NotEmpty(r.Value!.NextRuns);
    }
}