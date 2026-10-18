using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using Xunit;

namespace tool_cantam.Tests;

public sealed class GeoIpLookupServiceTests
{
    private readonly GeoIpLookupService _service = new();

    [Fact]
    public async Task LookupAsync_Empty_ReturnsValidation()
    {
        var r = await _service.LookupAsync("", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_InvalidIp_ReturnsValidation()
    {
        var r = await _service.LookupAsync("not-an-ip", CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task LookupAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.LookupAsync("8.8.8.8", cts.Token);
        Assert.False(r.IsSuccess);
    }

    // Not: Gerçek ağ testleri yapılmaz (ip-api.com rate limit)
}