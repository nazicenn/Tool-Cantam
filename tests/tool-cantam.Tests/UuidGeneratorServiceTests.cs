using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class UuidGeneratorServiceTests
{
    private readonly UuidGeneratorService _service = new();

    private static UuidOptions Opts(
        int count = 1,
        UuidCase c = UuidCase.Upper,
        bool hyphens = true,
        bool braces = false)
        => new(count, c, hyphens, braces);

    [Fact]
    public async Task GenerateAsync_Valid_ReturnsCount()
    {
        var r = await _service.GenerateAsync(Opts(3), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value!.Items.Count);
    }

    [Fact]
    public async Task GenerateAsync_LowerCase_FormatsLower()
    {
        var r = await _service.GenerateAsync(Opts(1, UuidCase.Lower), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(r.Value!.Items[0], r.Value.Items[0].ToLowerInvariant());
    }

    [Fact]
    public async Task GenerateAsync_NoHyphens_HasNoDash()
    {
        var r = await _service.GenerateAsync(Opts(1, hyphens: false), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.DoesNotContain("-", r.Value!.Items[0]);
    }

    [Fact]
    public async Task GenerateAsync_WithBraces_HasBraces()
    {
        var r = await _service.GenerateAsync(Opts(1, braces: true), CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.StartsWith("{", r.Value!.Items[0]);
        Assert.EndsWith("}", r.Value.Items[0]);
    }

    [Fact]
    public async Task GenerateAsync_CountZero_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(0), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_CountTooHigh_ReturnsValidation()
    {
        var r = await _service.GenerateAsync(Opts(UuidGeneratorService.MaxCountLimit + 1), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task GenerateAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.GenerateAsync(Opts(1000), cts.Token);
        Assert.False(r.IsSuccess);
    }
}