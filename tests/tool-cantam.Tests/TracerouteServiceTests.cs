using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class TracerouteServiceTests
{
    private readonly TracerouteService _service = new();

    [Fact]
    public async Task TraceAsync_Empty_ReturnsValidation()
    {
        var r = await _service.TraceAsync("", 30, null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task TraceAsync_InvalidHops_ReturnsValidation()
    {
        var r = await _service.TraceAsync("8.8.8.8", 0, null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task TraceAsync_InvalidHost_ReturnsNotFound()
    {
        var r = await _service.TraceAsync("this.host.does.not.exist.invalid", 5, null, CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.NotFound, r.ErrorKind);
    }

    [Fact]
    public async Task TraceAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.TraceAsync("8.8.8.8", 30, null, cts.Token);
        Assert.False(r.IsSuccess);
    }

    // Gerçek traceroute testi yapılmaz (yönetici izni + yavaş)
}