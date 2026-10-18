using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace tool_cantam.Tests;

public sealed class HttpRequestTesterServiceTests
{
    private readonly HttpRequestTesterService _service = new();

    [Fact]
    public async Task SendAsync_EmptyUrl_ReturnsValidation()
    {
        var r = await _service.SendAsync(new HttpTesterOptions("", "GET", "", null), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task SendAsync_InvalidUrl_ReturnsValidation()
    {
        var r = await _service.SendAsync(new HttpTesterOptions("not-a-url", "GET", "", null), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task SendAsync_InvalidMethod_ReturnsValidation()
    {
        var r = await _service.SendAsync(new HttpTesterOptions("https://example.com", "INVALID", "", null), CancellationToken.None);
        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
    }

    [Fact]
    public async Task SendAsync_Cancelled_ReturnsFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await _service.SendAsync(new HttpTesterOptions("https://example.com", "GET", "", null), cts.Token);
        Assert.False(r.IsSuccess);
    }
}