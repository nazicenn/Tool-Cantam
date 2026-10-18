using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class UnixTimestampServiceTests
{
    private readonly UnixTimestampService _service = new();

    [Fact]
    public async Task FromUnix_ValidSeconds_ReturnsOk()
    {
        var r = await _service.FromUnixAsync("1733673600", TimestampUnit.Seconds, TimeDisplayMode.Utc, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(1733673600L, r.Value!.UnixSeconds);
    }

    [Fact]
    public async Task FromUnix_ValidMillis_ReturnsOk()
    {
        var r = await _service.FromUnixAsync("1733673600000", TimestampUnit.Milliseconds, TimeDisplayMode.Utc, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.Equal(1733673600L, r.Value!.UnixSeconds);
    }

    [Fact]
    public async Task FromUnix_EmptyInput_ReturnsValidation()
    {
        var r = await _service.FromUnixAsync("", TimestampUnit.Seconds, TimeDisplayMode.Utc, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task FromUnix_InvalidNumber_ReturnsValidation()
    {
        var r = await _service.FromUnixAsync("abc", TimestampUnit.Seconds, TimeDisplayMode.Utc, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task FromUnix_OutOfRange_ReturnsValidation()
    {
        var r = await _service.FromUnixAsync("999999999999999", TimestampUnit.Seconds, TimeDisplayMode.Utc, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ToUnix_ValidDate_ReturnsOk()
    {
        var r = await _service.ToUnixAsync("2024-12-08 20:00:00", TimeDisplayMode.Utc, CancellationToken.None);
        Assert.True(r.IsSuccess);
        Assert.True(r.Value!.UnixSeconds > 0);
    }

    [Fact]
    public async Task ToUnix_Empty_ReturnsValidation()
    {
        var r = await _service.ToUnixAsync("", TimeDisplayMode.Utc, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task ToUnix_InvalidDate_ReturnsValidation()
    {
        var r = await _service.ToUnixAsync("not-a-date", TimeDisplayMode.Utc, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public void Now_ReturnsPositive()
    {
        var r = _service.Now();
        Assert.True(r.UnixSeconds > 0);
        Assert.Equal(TimeDisplayMode.Utc, r.Mode);
    }

    [Fact]
    public async Task FromUnix_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.FromUnixAsync("0", TimestampUnit.Seconds, TimeDisplayMode.Utc, cts.Token);
        // Cancellation token iptal edilmiş; ya Cancelled ya da hızlıca geçer (race)
        Assert.False(r.IsSuccess);
    }
}