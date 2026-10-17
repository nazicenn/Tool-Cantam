using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using tool_cantam.Core.Models;
using tool_cantam.Services;
using tool_cantam.Services.Interfaces;
using Xunit;

namespace ToolCantam.Tests;

/// <summary>
/// IP Info servisinin sözleşmesini kilitler: IP doğrulama, HTTP hata eşlemesi,
/// iptal davranışı ve sonuç modeli.
/// </summary>
public class IpInfoServiceTests
{
    // ---------------------------------------------------------------------
    // IP doğrulama
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIp_reports_empty(string? ip)
    {
        var service = new IpInfoService();
        var r = service.ValidateIp(ip);

        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Validation, r.ErrorKind);
        Assert.Equal(IpInfoErrorCodes.InvalidIp, r.ErrorCode);
    }

    [Theory]
    [InlineData("8.8.8.8", "8.8.8.8")]
    [InlineData("  1.1.1.1  ", "1.1.1.1")]
    [InlineData("2001:4860:4860::8888", "2001:4860:4860::8888")]
    public void ValidateIp_accepts_valid(string input, string expected)
    {
        var service = new IpInfoService();
        var r = service.ValidateIp(input);

        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value);
    }

    [Theory]
    [InlineData("not an ip")]
    [InlineData("999.999.999.999")]
    [InlineData("google.com")]
    public void ValidateIp_rejects_malformed(string input)
    {
        var service = new IpInfoService();
        var r = service.ValidateIp(input);

        Assert.False(r.IsSuccess);
        Assert.Equal(IpInfoErrorCodes.InvalidIp, r.ErrorCode);
    }

    // ---------------------------------------------------------------------
    // HTTP hata eşlemesi (fake handler)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetIpInfoAsync_maps_429_to_RateLimited()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)));
        var service = new IpInfoService(new HttpClient(handler));

        var r = await service.GetIpInfoAsync("8.8.8.8", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(IpInfoErrorCodes.RateLimited, r.ErrorCode);
    }

    [Fact]
    public async Task GetIpInfoAsync_maps_500_to_NetworkError()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var service = new IpInfoService(new HttpClient(handler));

        var r = await service.GetIpInfoAsync("8.8.8.8", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(IpInfoErrorCodes.NetworkError, r.ErrorCode);
    }

    [Fact]
    public async Task GetIpInfoAsync_maps_fail_status_to_NotFound()
    {
        string body = "{\"status\":\"fail\",\"message\":\"reserved range\",\"query\":\"10.0.0.1\"}";
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }));
        var service = new IpInfoService(new HttpClient(handler));

        var r = await service.GetIpInfoAsync("10.0.0.1", TestContext.Current.CancellationToken);

        Assert.False(r.IsSuccess);
        Assert.Equal(IpInfoErrorCodes.NotFound, r.ErrorCode);
    }

    [Fact]
    public async Task GetIpInfoAsync_maps_success_body_to_report()
    {
        string body = """
            {
              "status": "success",
              "country": "United States",
              "countryCode": "US",
              "region": "CA",
              "regionName": "California",
              "city": "Mountain View",
              "zip": "94043",
              "lat": 37.4056,
              "lon": -122.0775,
              "timezone": "America/Los_Angeles",
              "isp": "Google LLC",
              "org": "Google Public DNS",
              "as": "AS15169 Google LLC",
              "query": "8.8.8.8"
            }
            """;
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            }));
        var service = new IpInfoService(new HttpClient(handler));

        var r = await service.GetIpInfoAsync("8.8.8.8", TestContext.Current.CancellationToken);

        Assert.True(r.IsSuccess);
        var report = r.Value!;
        Assert.Equal("8.8.8.8", report.IpAddress);
        Assert.Equal("IPv4", report.AddressFamily);
        Assert.Equal("United States", report.Country);
        Assert.Equal("California", report.Region);
        Assert.Equal("Mountain View", report.City);
        Assert.Equal("Google LLC", report.Isp);
        Assert.Equal("AS15169", report.Asn);
    }

    [Fact]
    public async Task GetIpInfoAsync_honours_cancellation()
    {
        var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(2000, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new IpInfoService(new HttpClient(handler));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        cts.Cancel();

        var r = await service.GetIpInfoAsync("8.8.8.8", cts.Token);

        Assert.False(r.IsSuccess);
        Assert.Equal(ToolErrorKind.Cancelled, r.ErrorKind);
        Assert.Equal(IpInfoErrorCodes.Cancelled, r.ErrorCode);
    }

    [Fact]
    public void Service_exposes_safe_limits()
    {
        var service = new IpInfoService();
        Assert.True(service.QueryTimeoutMs > 0);
        Assert.True(service.QueryTimeoutMs <= 10000);
    }
}

// =====================================================================
// Fake HTTP handler
// =====================================================================

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly System.Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(System.Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}