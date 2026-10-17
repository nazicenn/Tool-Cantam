using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.ErrorCodes;
using Xunit;

namespace ToolCantam.Tests;

public class SubdomainServiceTests
{
    private readonly SubdomainService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDomain_reports_empty_input(string? domain)
    {
        var result = _service.ValidateDomain(domain);
        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
        Assert.Equal(SubdomainErrorCodes.InvalidDomain, result.ErrorCode);
    }

    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("  EXAMPLE.COM  ", "example.com")]
    [InlineData("sub.example.com", "sub.example.com")]
    [InlineData("test-site.co.uk", "test-site.co.uk")]
    public void ValidateDomain_normalizes_and_accepts_valid_domains(string input, string expected)
    {
        var result = _service.ValidateDomain(input);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("example com")]
    [InlineData("example.com/path")]
    [InlineData("example.com:8080")]
    [InlineData("not_a_domain")]
    public void ValidateDomain_rejects_malformed_input(string input)
    {
        var result = _service.ValidateDomain(input);
        Assert.False(result.IsSuccess);
        Assert.Equal(SubdomainErrorCodes.InvalidDomain, result.ErrorCode);
    }

    [Fact]
    public void ValidateDomain_rejects_domain_over_253_chars()
    {
        string tooLong = new string('a', 254) + ".com";
        var result = _service.ValidateDomain(tooLong);
        Assert.False(result.IsSuccess);
        Assert.Equal(SubdomainErrorCodes.InvalidDomain, result.ErrorCode);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        Assert.True(_service.MaxCandidatesPerScan > 0);
        Assert.True(_service.MaxCandidatesPerScan <= 10000);
        Assert.True(_service.MaxConcurrency > 0);
        Assert.True(_service.MaxConcurrency <= 100);
        Assert.True(_service.QueryTimeoutMs >= 100);
        Assert.True(_service.QueryTimeoutMs <= 10000);
    }

    [Fact]
    public async Task ScanAsync_rejects_invalid_domain_without_network_call()
    {
        var result = await _service.ScanAsync(
            "invalid domain with spaces",
            null,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubdomainErrorCodes.InvalidDomain, result.ErrorCode);
        Assert.Equal(ToolErrorKind.Validation, result.ErrorKind);
    }

    [Fact]
    public async Task ScanAsync_rejects_null_domain()
    {
        var result = await _service.ScanAsync(
            null,
            null,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubdomainErrorCodes.InvalidDomain, result.ErrorCode);
    }

    [Fact]
    public async Task ScanAsync_honours_cancellation_before_work_starts()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var result = await _service.ScanAsync("example.com", null, cts.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, result.ErrorKind);
        Assert.Equal(SubdomainErrorCodes.Cancelled, result.ErrorCode);
    }
}