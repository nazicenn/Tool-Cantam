using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

public class PingServiceTests
{
    private readonly PingService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTarget_reports_empty(string? target)
    {
        var r = _service.ValidateTarget(target);
        Assert.False(r.IsSuccess);
        Assert.Equal(PingErrorCodes.InvalidTarget, r.ErrorCode);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("  8.8.8.8  ", "8.8.8.8")]
    [InlineData("google.com", "google.com")]
    public void ValidateTarget_accepts_valid(string input, string expected)
    {
        var r = _service.ValidateTarget(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("bad domain")]
    [InlineData("example.com/path")]
    [InlineData("not_a_domain")]
    public void ValidateTarget_rejects_malformed(string input)
    {
        var r = _service.ValidateTarget(input);
        Assert.False(r.IsSuccess);
        Assert.Equal(PingErrorCodes.InvalidTarget, r.ErrorCode);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.MaxCount > 0);
        Assert.True(_service.MaxCount <= 50);
        Assert.True(_service.MinTimeoutMs > 0);
        Assert.True(_service.MaxTimeoutMs <= 20000);
    }

    [Fact]
    public async Task PingAsync_rejects_invalid_target()
    {
        var r = await _service.PingAsync(
            "bad domain with spaces", 4, 2000, null,
            TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(PingErrorCodes.InvalidTarget, r.ErrorCode);
    }

    [Theory]
    [InlineData(0, 2000)]
    [InlineData(100, 2000)]
    public async Task PingAsync_rejects_invalid_count(int count, int timeoutMs)
    {
        var r = await _service.PingAsync(
            "example.com", count, timeoutMs, null,
            TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(PingErrorCodes.InvalidCount, r.ErrorCode);
    }

    [Theory]
    [InlineData(4, 50)]
    [InlineData(4, 99999)]
    public async Task PingAsync_rejects_invalid_timeout(int count, int timeoutMs)
    {
        var r = await _service.PingAsync(
            "example.com", count, timeoutMs, null,
            TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(PingErrorCodes.InvalidTimeout, r.ErrorCode);
    }

    [Fact]
    public async Task PingAsync_honours_cancellation()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var r = await _service.PingAsync("example.com", 4, 2000, null, cts.Token);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, r.ErrorKind);
    }
}