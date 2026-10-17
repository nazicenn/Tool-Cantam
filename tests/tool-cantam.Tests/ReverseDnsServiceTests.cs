using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using Xunit;

namespace ToolCantam.Tests;

public class ReverseDnsServiceTests
{
    private readonly ReverseDnsService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIp_reports_empty(string? ip)
    {
        var r = _service.ValidateIp(ip);
        Assert.False(r.IsSuccess);
        Assert.Equal(ReverseDnsErrorCodes.InvalidIp, r.ErrorCode);
    }

    [Theory]
    [InlineData("8.8.8.8", "8.8.8.8")]
    [InlineData("  1.1.1.1  ", "1.1.1.1")]
    [InlineData("2001:4860:4860::8888", "2001:4860:4860::8888")]
    public void ValidateIp_accepts_valid(string input, string expected)
    {
        var r = _service.ValidateIp(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("not an ip")]
    [InlineData("999.999.999.999")]
    [InlineData("google.com")]
    public void ValidateIp_rejects_malformed(string input)
    {
        var r = _service.ValidateIp(input);
        Assert.False(r.IsSuccess);
        Assert.Equal(ReverseDnsErrorCodes.InvalidIp, r.ErrorCode);
    }

    [Fact]
    public async Task LookupAsync_rejects_invalid_ip()
    {
        var r = await _service.LookupAsync("not an ip", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(ReverseDnsErrorCodes.InvalidIp, r.ErrorCode);
    }

    [Fact]
    public async Task LookupAsync_honours_cancellation()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var r = await _service.LookupAsync("8.8.8.8", cts.Token);

        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, r.ErrorKind);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.QueryTimeoutMs > 0);
        Assert.True(_service.QueryTimeoutMs <= 10000);
    }
}