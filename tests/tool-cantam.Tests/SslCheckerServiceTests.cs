using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

public class SslCheckerServiceTests
{
    private readonly SslCheckerService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDomain_reports_empty(string? domain)
    {
        var r = _service.ValidateDomain(domain);
        Assert.False(r.IsSuccess);
        Assert.Equal(SslCheckerErrorCodes.InvalidDomain, r.ErrorCode);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("  GOOGLE.COM  ", "google.com")]
    public void ValidateDomain_accepts_valid(string input, string expected)
    {
        var r = _service.ValidateDomain(input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("bad domain")]
    [InlineData("example.com/path")]
    [InlineData("not_a_domain")]
    public void ValidateDomain_rejects_malformed(string input)
    {
        var r = _service.ValidateDomain(input);
        Assert.False(r.IsSuccess);
        Assert.Equal(SslCheckerErrorCodes.InvalidDomain, r.ErrorCode);
    }

    [Fact]
    public async Task CheckAsync_rejects_invalid_domain()
    {
        var r = await _service.CheckAsync("bad domain with spaces", TestContext.Current.CancellationToken);
        Assert.False(r.IsSuccess);
        Assert.Equal(SslCheckerErrorCodes.InvalidDomain, r.ErrorCode);
    }

    [Fact]
    public async Task CheckAsync_honours_cancellation()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var r = await _service.CheckAsync("example.com", cts.Token);

        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, r.ErrorKind);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.QueryTimeoutMs > 0);
        Assert.True(_service.QueryTimeoutMs <= 15000);
    }
}